using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.ThreadPools.ServiceStuff
{
    /// <summary>
    /// Состояния ExecutionThroughoutTrackerUpSmartDownCorrection
    /// </summary>
    internal enum ExecutionThroughoutTrackerUpSmartDownCorrectionState
    {
        InOptimalState,
        TryIncrease,
        Increasing,
        TryDecrease,
        Decreasing
    }

    /// <summary>
    /// Трекер скорости исполнения задач.
    /// В текущей точке определяет лучшее направление изменения числа потоков.
    /// Если требуется уменьшение, то рассчитывает примерное число требуемых потоков и сбрасывает до этого значения, а дальше выполняет их увеличение.
    /// (Сводим задачу уменьшеня к более стабильной задаче увеличения числа потоков)
    /// </summary>
    internal class ExecutionThroughoutTrackerUpSmartDownCorrection
    {
        private const int OptimalStateTime = 15 * 1000;
        public static double OptimalStateAverageCaptureLocalFluctuationDiffCoef = 0.15;
        public static double OptimalStateFluctuationDiffCoef = 0.05;
 
        public static double TryIncreaseSuggestIncreaseDiff = 0.20;
     
        public static double IncreaseDirectionLocGoodDiff = 0.25;
        public static double IncreaseDirectionAvgGoodDiff = 0.2;
        public static double IncreaseDirectionLocDropDiff = -0.04;
        public static double IncreaseDirectionAvgDropDiff = -0.02;


        private const double FluctuationDiffCoef = 0.02;
        private const double HardDropFluctuationDiffCoef = 0.1;

        private const int EstimationDataLength = 4;
        private const int PerfTrackingHarmonic = 2;
        private const int PerfThreadCount = 1;

        // ===================

        /// <summary>
        /// Данные по каждой регистрации
        /// </summary>
        private struct ThroughoutData
        {
            public ThroughoutData(int executedTaskCount, double throughout, double averageThroughout, int threadCount, bool isPerfMeasureThreadWork)
            {
                Contract.Requires(executedTaskCount >= 0);
                Contract.Requires(throughout >= 0);
                Contract.Requires(averageThroughout >= 0);
                Contract.Requires(threadCount >= 0);

                ExecutedTaskCount = executedTaskCount;
                Throughout = throughout;
                AverageThroughout = averageThroughout;
                ThreadCount = threadCount;
                IsPerfMeasureThreadWork = isPerfMeasureThreadWork;
            }

            public readonly int ExecutedTaskCount;

            public readonly double Throughout;
            public readonly double AverageThroughout;

            public readonly int ThreadCount;
            public readonly bool IsPerfMeasureThreadWork;

            public int IgnorePerfMeasureThreadCount { get { return IsPerfMeasureThreadWork ? (ThreadCount - PerfThreadCount) : ThreadCount; } }
        }

        // ===============

        /// <summary>
        /// Получить временной маркер в миллисекундах
        /// </summary>
        /// <returns>Временной маркер</returns>
        private static uint GetTimestamp()
        {
            return (uint)Environment.TickCount;
        }
        /// <summary>
        /// Близки ли числа с заданной точностью
        /// </summary>
        /// <param name="a">Число A</param>
        /// <param name="b">Число B</param>
        /// <param name="precision">Точность</param>
        /// <returns>Близки ли</returns>
        private static bool AreClose(double a, double b, double precision = 1E-8)
        {
            return Math.Abs(a - b) < precision;
        }
        /// <summary>
        /// Является ли указанное изменение флуктуацией с заданным коэффициентом.
        /// Вычисляет разницу, нормированную на среднее
        /// </summary>
        /// <param name="curThroughout">Текущее значение</param>
        /// <param name="prevThoughout">Предыдущее значение</param>
        /// <param name="fluctuationDiffCoef">Коэффициент флуктуации (2/3 для изменения в 2 раза)</param>
        /// <returns>Является ли флуктуацией</returns>
        private static bool IsThroughoutFluctuation(double curThroughout, double prevThoughout, double fluctuationDiffCoef = FluctuationDiffCoef)
        {
            return (2.0 * Math.Abs(curThroughout - prevThoughout) / (curThroughout + prevThoughout)) < fluctuationDiffCoef;
        }


        /// <summary>
        /// Вычислить среднюю производительность
        /// </summary>
        /// <param name="data">Замеры</param>
        /// <returns>Среднее</returns>
        private static double CalcAverageThroughout(ThroughoutData[] data)
        {
            Contract.Requires(data != null);
            Contract.Requires(data.Length > 0);

            double res = 0;
            for (int i = 0; i < data.Length; i++)
                res += data[i].Throughout;
            return res / data.Length;
        }
        /// <summary>
        /// Вычислить среднюю производительность по элемнтам с указанным шагом (index % n == k)
        /// </summary>
        /// <param name="data">Замеры</param>
        /// <param name="startIndex">Индекс начала</param>
        /// <param name="k">Индекс </param>
        /// <param name="n">Размер блока</param>
        /// <returns>Среднее</returns>
        private static double CalcAverageThroughoutForSomeElements(ThroughoutData[] data, int startIndex, int k, int n)
        {
            Contract.Requires(data != null);
            Contract.Requires(data.Length > 0);
            Contract.Requires(startIndex >= 0 && startIndex < data.Length);
            Contract.Requires(n > 0 && n < data.Length);
            Contract.Requires(k >= 0 && k < n);

            double throughoutSum = 0;
            int valCount = 0;
            int curIndex = startIndex;
            for (int i = 0; i < data.Length; i++)
            {
                if ((i % n) == k)
                {
                    throughoutSum += data[curIndex].Throughout;
                    valCount++;
                }
                curIndex = (curIndex + 1) % data.Length;
            }

            return throughoutSum / valCount;
        }

        /// <summary>
        /// Оценить ожидаемую производительность при изменении числа потоков (влияние считается линейным)
        /// </summary>
        /// <param name="curThreadCount">Предыдущее число потоков</param>
        /// <param name="curThroughout">Предыдущая производительность</param>
        /// <param name="newThreadCount">Новое число потоков</param>
        /// <returns>Оценочная производительность</returns>
        private static double EstimateExpectedThroughout(int curThreadCount, double curThroughout, int newThreadCount)
        {
            Contract.Requires(curThreadCount >= 0);
            Contract.Requires(curThroughout >= 0);
            Contract.Requires(newThreadCount >= 0);

            if (curThreadCount == 0)
                return 1.0;

            return (curThroughout / curThreadCount) * newThreadCount;
        }
        /// <summary>
        /// Оценить коэффициент изменения производительности относительно ожидаемого изменения ((new - cur) / (estimNew - cur))
        /// </summary>
        /// <param name="curThreadCount">Предыдущее число потоков</param>
        /// <param name="curThroughout">Предыдущая производительность</param>
        /// <param name="newThreadCount">Новое число потоков</param>
        /// <param name="newThroughout">Новая производительность</param>
        /// <returns>Коэффициент</returns>
        private static double EstimateThroughoutDiffCoef(int curThreadCount, double curThroughout, int newThreadCount, double newThroughout)
        {
            Contract.Requires(curThreadCount >= 0);
            Contract.Requires(curThroughout >= 0);
            Contract.Requires(newThreadCount >= 0);
            Contract.Requires(newThroughout >= 0);

            double estimatedThroughout = EstimateExpectedThroughout(curThreadCount, curThroughout, newThreadCount);
            return (newThroughout - curThroughout) / (estimatedThroughout - curThroughout);
        }
        /// <summary>
        /// Оценить амплитуду мигания потоками
        /// </summary>
        /// <param name="baseThreadCount">Базовое число потоков</param>
        /// <param name="blinkThreadCount">Число мигающих потоков</param>
        /// <param name="avgThroughout">Средняя производительность</param>
        /// <returns>Оценочная амплитуда</returns>
        private static double EstimateAmplitudeForBlinking(int baseThreadCount, int blinkThreadCount, double avgThroughout)
        {
            Contract.Requires(baseThreadCount >= 0);
            Contract.Requires(blinkThreadCount > 0);
            Contract.Requires(avgThroughout >= 0);

            return avgThroughout * blinkThreadCount / (2 * baseThreadCount + blinkThreadCount);
        }

        /// <summary>
        /// Вычислить k-ую гармонику по Фурье для производительности
        /// </summary>
        /// <param name="data">Замеры</param>
        /// <param name="startIndex">Индекс начала</param>
        /// <param name="k">Номер гармоники</param>
        /// <param name="xRe">Действительная часть гармоники</param>
        /// <param name="xIm">Мнимая часть гармоники</param>
        private static void CalcFourierThoughoutHarmonic(ThroughoutData[] data, int startIndex, int k, out double xRe, out double xIm)
        {
            Contract.Requires(data != null);
            Contract.Requires(data.Length > 0);
            Contract.Requires(startIndex >= 0 && startIndex < data.Length);
            Contract.Requires(k >= 0 && k < data.Length);

            double xReLoc = 0, xImLoc = 0;

            double N = data.Length;
            int curDataIndex = startIndex;
            for (int i = 0; i < data.Length; i++)
            {
                xReLoc += data[curDataIndex].Throughout * Math.Cos(-2.0 * Math.PI * k * i / N);
                xImLoc += data[curDataIndex].Throughout * Math.Sin(-2.0 * Math.PI * k * i / N);

                curDataIndex = (curDataIndex + 1) % data.Length;
            }

            xRe = xReLoc;
            xIm = xImLoc;
        }
        /// <summary>
        /// Вычислить k-ую гармонику по Фурье для числа потков
        /// </summary>
        /// <param name="data">Замеры</param>
        /// <param name="startIndex">Индекс начала</param>
        /// <param name="k">Номер гармоники</param>
        /// <param name="xRe">Действительная часть гармоники</param>
        /// <param name="xIm">Мнимая часть гармоники</param>
        private static void CalcFourierThreadCountHarmonic(ThroughoutData[] data, int startIndex, int k, out double xRe, out double xIm)
        {
            Contract.Requires(data != null);
            Contract.Requires(data.Length > 0);
            Contract.Requires(startIndex >= 0 && startIndex < data.Length);
            Contract.Requires(k >= 0 && k < data.Length);

            double xReLoc = 0, xImLoc = 0;

            double N = data.Length;
            int curDataIndex = startIndex;
            for (int i = 0; i < data.Length; i++)
            {
                xReLoc += data[curDataIndex].ThreadCount * Math.Cos(-2.0 * Math.PI * k * i / N);
                xImLoc += data[curDataIndex].ThreadCount * Math.Sin(-2.0 * Math.PI * k * i / N);

                curDataIndex = (curDataIndex + 1) % data.Length;
            }

            xRe = xReLoc;
            xIm = xImLoc;
        }
        /// <summary>
        /// Расчитать амплитуду гармоники
        /// </summary>
        /// <param name="xRe">Действительная часть</param>
        /// <param name="xIm">Мнимая часть</param>
        /// <param name="N">Число измерений</param>
        /// <returns>Амплитуда</returns>
        private static double CalcAmplitude(double xRe, double xIm, int N = EstimationDataLength)
        {
            return Math.Sqrt(xRe * xRe + xIm * xIm) / N;
        }
        /// <summary>
        /// Вычислить фазу гармоники
        /// </summary>
        /// <param name="xRe">Действительная часть</param>
        /// <param name="xIm">Мнимая часть</param>
        /// <returns>Фаза</returns>
        private static double CalcPhase(double xRe, double xIm)
        {
            return Math.Atan2(xIm, xRe);
        }

        // ===============


        private readonly int _reasonableThreadCount;
        private readonly int _maxThreadCount;

        private int _executedTasks;
        private uint _lastTimeStamp;

        private readonly ThroughoutData[] _data;
        private int _nextDataIndex;

        private ExecutionThroughoutTrackerUpSmartDownCorrectionState _state;
        private ThroughoutData _enterStateThroughoutData;
        private uint _enterStateTimeStamp;
        private uint _stateMeasureCount;

        private int _tryIncreaseBlinkCount;
        private int _tryDecreaseBlinkCount;
        private int _tryDecreaseInitialThreadCount;
        private double _optimalStateAverageThroughout;

        private volatile bool _isPerfMeasureThreadWork;
        

        /// <summary>
        /// Конструктор ExecutionThroughoutTrackerUpSmartDownCorrection
        /// </summary>
        /// <param name="maxThreadCount">Максимальное число потоков</param>
        /// <param name="reasonableThreadCount">Базовое число потоков</param>
        public ExecutionThroughoutTrackerUpSmartDownCorrection(int maxThreadCount, int reasonableThreadCount)
        {
            Contract.Requires(maxThreadCount > 0);
            Contract.Requires(reasonableThreadCount > 0);
            Contract.Requires(maxThreadCount >= reasonableThreadCount);

            _maxThreadCount = maxThreadCount;
            _reasonableThreadCount = reasonableThreadCount;

            _executedTasks = 0;
            _lastTimeStamp = GetTimestamp();

            _data = new ThroughoutData[EstimationDataLength];
            _nextDataIndex = 0;

            _state = ExecutionThroughoutTrackerUpSmartDownCorrectionState.InOptimalState;
            _enterStateTimeStamp = GetTimestamp();
            _enterStateThroughoutData = new ThroughoutData();
            _stateMeasureCount = 0;

            _tryIncreaseBlinkCount = 0;
            _tryDecreaseBlinkCount = 0;
            _tryDecreaseInitialThreadCount = -1;
            _optimalStateAverageThroughout = -1;

            _isPerfMeasureThreadWork = false;
        }

        /// <summary>
        /// Состояние
        /// </summary>
        public ExecutionThroughoutTrackerUpSmartDownCorrectionState State { get { return _state; } }


        private ThroughoutData GetCurrentMeasure()
        {
            return _data[(_nextDataIndex + _data.Length - 1) % _data.Length];
        }
        private ThroughoutData GetPrevMeasure()
        {
            return _data[(_nextDataIndex + _data.Length - 2) % _data.Length];
        }
        private ThroughoutData GetPrevPrevMeasure()
        {
            return _data[(_nextDataIndex + _data.Length - 3) % _data.Length];
        }


        /// <summary>
        /// Зарегистрировать исполнение одной задачи
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RegisterExecution()
        {
            Interlocked.Increment(ref _executedTasks);
        }
        private void RegisterExecution(int count)
        {
            Interlocked.Add(ref _executedTasks, count);
        }

        /// <summary>
        /// Зарегистрировать измерение
        /// </summary>
        /// <param name="workThreadCount">Текущее число потоков</param>
        /// <param name="isPerfThreadActive">Работает ли сейчас поток замера производительности</param>
        /// <param name="elapsedMs">Прошедшее время (для отладки, учитывается при значениях больше 0)</param>
        /// <returns>Выполненный замер</returns>
        private ThroughoutData RegisterMeasure(int workThreadCount, bool isPerfThreadActive, int elapsedMs = -1)
        {
            Contract.Requires(workThreadCount >= 0);

            int executedTasks = Interlocked.Exchange(ref _executedTasks, 0);
            var currentTime = GetTimestamp();
            var elapsedTime = currentTime - _lastTimeStamp;
            if (elapsedMs > 0)
                elapsedTime = (uint)elapsedMs;
            double throughout = (double)executedTasks / elapsedTime;
            double newThroughoutAvg = (_data.Sum(o => o.Throughout) - _data[_nextDataIndex].Throughout + throughout) / _data.Length;
            var result = new ThroughoutData(executedTasks, throughout, newThroughoutAvg, workThreadCount, isPerfThreadActive);

            _lastTimeStamp = currentTime;
            _data[_nextDataIndex] = result;
            _nextDataIndex = (_nextDataIndex + 1) % _data.Length;
            _stateMeasureCount++;

            return result;
        }


        // ==================

        private void CalcFourierTest()
        {
            var curMeasure = GetCurrentMeasure();
            Console.WriteLine("ThreadCount = " + curMeasure.ThreadCount.ToString() + ", throughout = " + curMeasure.Throughout.ToString());

            for (int i = 0; i < EstimationDataLength; i++)
            {
                // if (i == 0 || i == 4)
                {
                    double xRe = 0, xIm = 0;
                    CalcFourierThoughoutHarmonic(_data, _nextDataIndex, i, out xRe, out xIm);

                    Console.WriteLine(string.Format("x{0} = {1:0.#####} + {2:0.#####}i, x{0}_amp = {3:0.#####}, x{0}_phase = {4:0.#####}", i, xRe, xIm, CalcAmplitude(xRe, xIm), CalcPhase(xRe, xIm)));
                }
            }


            for (int i = 0; i < EstimationDataLength; i++)
            {
                // if (i == 0 || i == 4)
                {
                    double xRe = 0, xIm = 0;
                    CalcFourierThreadCountHarmonic(_data, _nextDataIndex, i, out xRe, out xIm);

                    Console.WriteLine(string.Format("x{0}_th = {1:0.#####} + {2:0.#####}i, x{0}_th_amp = {3:0.#####}, x{0}_th_phase = {4:0.#####}", i, xRe, xIm, CalcAmplitude(xRe, xIm), CalcPhase(xRe, xIm)));
                }
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Расчитать основные метрики фурье
        /// </summary>
        /// <param name="throughoutPerfHarmAmp">Изменение производительности при мигании потоками</param>
        /// <param name="threadCountPerfHarmAmp">Величина мигания потоками (в норме должно быть равно половине мигающих потоков)</param>
        private void CalcFourierMetrics(out double throughoutPerfHarmAmp, out double threadCountPerfHarmAmp)
        {
            double throughoutPerfHarm_re = 0, throughoutPerfHarm_im = 0;
            CalcFourierThoughoutHarmonic(_data, _nextDataIndex, PerfTrackingHarmonic, out throughoutPerfHarm_re, out throughoutPerfHarm_im);
            throughoutPerfHarmAmp = CalcAmplitude(throughoutPerfHarm_re, throughoutPerfHarm_im);
            //throughoutPerfHarmPhase = CalcPhase(throughoutPerfHarm_re, throughoutPerfHarm_im);


            double threadCountPerfHarm_re = 0, threadCountPerfHarm_im = 0;
            CalcFourierThreadCountHarmonic(_data, _nextDataIndex, PerfTrackingHarmonic, out threadCountPerfHarm_re, out threadCountPerfHarm_im);
            threadCountPerfHarmAmp = CalcAmplitude(threadCountPerfHarm_re, threadCountPerfHarm_im);
            //threadCountPerfHarmPhase = CalcPhase(threadCountPerfHarm_re, threadCountPerfHarm_im);
        }

        // =========================


        /// <summary>
        /// Допустим ли переход между состояниями
        /// </summary>
        /// <param name="curState">Исходное состояние</param>
        /// <param name="newState">Новое состояние</param>
        /// <returns>Допустимость</returns>
        private bool CanChangeState(ExecutionThroughoutTrackerUpSmartDownCorrectionState curState, ExecutionThroughoutTrackerUpSmartDownCorrectionState newState)
        {
            if (curState == newState)
                return true;

            switch (curState)
            {
                case ExecutionThroughoutTrackerUpSmartDownCorrectionState.InOptimalState:
                    return newState == ExecutionThroughoutTrackerUpSmartDownCorrectionState.TryIncrease || newState == ExecutionThroughoutTrackerUpSmartDownCorrectionState.TryDecrease;
                case ExecutionThroughoutTrackerUpSmartDownCorrectionState.TryIncrease:
                    return newState == ExecutionThroughoutTrackerUpSmartDownCorrectionState.Increasing || newState == ExecutionThroughoutTrackerUpSmartDownCorrectionState.TryDecrease || newState == ExecutionThroughoutTrackerUpSmartDownCorrectionState.InOptimalState;
                case ExecutionThroughoutTrackerUpSmartDownCorrectionState.Increasing:
                    return newState == ExecutionThroughoutTrackerUpSmartDownCorrectionState.InOptimalState || newState == ExecutionThroughoutTrackerUpSmartDownCorrectionState.TryIncrease || newState == ExecutionThroughoutTrackerUpSmartDownCorrectionState.TryDecrease;
                case ExecutionThroughoutTrackerUpSmartDownCorrectionState.TryDecrease:
                    return newState == ExecutionThroughoutTrackerUpSmartDownCorrectionState.Decreasing || newState == ExecutionThroughoutTrackerUpSmartDownCorrectionState.TryIncrease || newState == ExecutionThroughoutTrackerUpSmartDownCorrectionState.InOptimalState;
                case ExecutionThroughoutTrackerUpSmartDownCorrectionState.Decreasing:
                    return newState == ExecutionThroughoutTrackerUpSmartDownCorrectionState.InOptimalState || newState == ExecutionThroughoutTrackerUpSmartDownCorrectionState.TryIncrease || newState == ExecutionThroughoutTrackerUpSmartDownCorrectionState.TryDecrease;
                default:
                    throw new InvalidOperationException("Unknown state: " + curState.ToString());
            }
        }

        /// <summary>
        /// Выполнить переход в состояние
        /// </summary>
        /// <param name="description">Описание причин перехода</param>
        /// <param name="state">Новое состояние</param>
        private void GoToState(string description, ExecutionThroughoutTrackerUpSmartDownCorrectionState state)
        {
            if (!CanChangeState(_state, state))
                throw new InvalidOperationException("Invalid state change from " + _state.ToString() + " to " + state.ToString());

            //Console.WriteLine(description + ". State = " + _state.ToString() + ", NewState = " + state.ToString());

            if (_state != state)
            {
                _state = state;

                _enterStateTimeStamp = GetTimestamp();
                _enterStateThroughoutData = this.GetCurrentMeasure();
                _stateMeasureCount = 0;

                _tryIncreaseBlinkCount = 0;
                _tryDecreaseBlinkCount = 0;
                _tryDecreaseInitialThreadCount = -1;
                _optimalStateAverageThroughout = -1;

                Debug.Assert(_isPerfMeasureThreadWork == false);
            }
        }
        /// <summary>
        /// Вернуть предложение
        /// </summary>
        /// <param name="description">Описание причин возврата</param>
        /// <param name="suggestion">Предложение (будет возвращено без изменений)</param>
        /// <returns>Предложение</returns>
        private int ReturnSuggestion(string description, int suggestion)
        {
            //Console.WriteLine(description + ". State = " + _state.ToString() + ", Suggestion = " + suggestion.ToString());
            return suggestion;
        }


        /// <summary>
        /// Включить поток замера производительности
        /// </summary>
        /// <returns>Число потоков, на которое нужно изменить текущее число потоков</returns>
        private int EnablePerfMeasureThreadIfRequired()
        {
            if (_isPerfMeasureThreadWork)
                return 0;

            _isPerfMeasureThreadWork = true;
            return PerfThreadCount;
        }
        /// <summary>
        /// Выключить поток замера производительности
        /// </summary>
        /// <returns>Число потоков, на которое надо изменить</returns>
        private int DisablePerfMeasureThreadIfRequired()
        {
            if (!_isPerfMeasureThreadWork)
                return 0;

            _isPerfMeasureThreadWork = false;
            return -PerfThreadCount;
        }


        /// <summary>
        /// Сделать предложение по изменению числа потоков в OptimalState
        /// </summary>
        /// <returns>Предложение</returns>
        private int MakeSuggestionInOptimalState()
        {
            if (GetTimestamp() - _enterStateTimeStamp > OptimalStateTime)
            {
                GoToState("Out of Optimal by timeout", ExecutionThroughoutTrackerUpSmartDownCorrectionState.TryIncrease);
                return ReturnSuggestion("Out of Optimal by timeout", 0);
            }

            var curMeasure = GetCurrentMeasure();
            var prevMeasure = GetPrevMeasure();

            if (_optimalStateAverageThroughout < 0 && _stateMeasureCount >= EstimationDataLength)
            {
                if (_stateMeasureCount >= EstimationDataLength)
                {
                    _optimalStateAverageThroughout = curMeasure.AverageThroughout;
                }
                else if (!IsThroughoutFluctuation(curMeasure.Throughout, prevMeasure.Throughout, OptimalStateAverageCaptureLocalFluctuationDiffCoef))
                {
                    GoToState("Out of Optimal by large diff", ExecutionThroughoutTrackerUpSmartDownCorrectionState.TryIncrease);
                    return ReturnSuggestion("Out of Optimal by large diff", 0);
                }
            }

            if (_optimalStateAverageThroughout >= 0 && _stateMeasureCount >= EstimationDataLength &&
                !IsThroughoutFluctuation(curMeasure.AverageThroughout, _optimalStateAverageThroughout, OptimalStateFluctuationDiffCoef))
            {
                GoToState("Out of Optimal by AvgThroughout", ExecutionThroughoutTrackerUpSmartDownCorrectionState.TryIncrease);
                return ReturnSuggestion("Out of Optimal by AvgThroughout", 0);
            }

            return ReturnSuggestion("Optimal", 0);
        }


        /// <summary>
        /// Сделать предложение по изменению числа потоков в TryIncreaseState
        /// </summary>
        /// <returns>Предложение</returns>
        private int MakeSuggestionInTryIncreaseState()
        {
            var curMeasure = GetCurrentMeasure();
            var prevMeasure = GetPrevMeasure();

            if (curMeasure.ThreadCount >= _maxThreadCount)
            {
                GoToState("Can't increase. Upper bound", ExecutionThroughoutTrackerUpSmartDownCorrectionState.TryDecrease);
                return ReturnSuggestion("Can't increase. Upper bound", 0);
            }

            if (curMeasure.IgnorePerfMeasureThreadCount != prevMeasure.IgnorePerfMeasureThreadCount)
                _tryIncreaseBlinkCount = 0;

            if (_tryIncreaseBlinkCount != EstimationDataLength)
            {
                int resultThreadCountDiff = 0;
                if ((_tryIncreaseBlinkCount % 2) == 0)
                    resultThreadCountDiff = EnablePerfMeasureThreadIfRequired();
                else
                    resultThreadCountDiff = DisablePerfMeasureThreadIfRequired();

                _tryIncreaseBlinkCount++;
                return ReturnSuggestion("TryIncrease blinking test", resultThreadCountDiff);
            }


            double throughoutAmp = 0, threadCountAmp = 0;
            CalcFourierMetrics(out throughoutAmp, out threadCountAmp);

            double estimAmp = EstimateAmplitudeForBlinking(curMeasure.IgnorePerfMeasureThreadCount, PerfThreadCount, curMeasure.AverageThroughout);

            double avgThroughoutForUp = CalcAverageThroughoutForSomeElements(_data, _nextDataIndex, 0, 2);
            double avgThroughoutForDown = CalcAverageThroughoutForSomeElements(_data, _nextDataIndex, 1, 2);

            if (avgThroughoutForUp < avgThroughoutForDown || throughoutAmp < estimAmp * TryIncreaseSuggestIncreaseDiff)
            {
                GoToState("Increase not give good perf", ExecutionThroughoutTrackerUpSmartDownCorrectionState.TryDecrease);
                return ReturnSuggestion("Increase not give good perf", 0);
            }

            GoToState("Increase give good perf", ExecutionThroughoutTrackerUpSmartDownCorrectionState.Increasing);
            return ReturnSuggestion("Increase give good perf", 2);
        }


        /// <summary>
        /// Сделать предложение по изменению числа потоков в IncreasingState
        /// </summary>
        /// <returns>Предложение</returns>
        private int MakeSuggestionInIncreasingState()
        {
            var curMeasure = GetCurrentMeasure();
            var prevMeasure = GetPrevMeasure();

            if (curMeasure.ThreadCount <= prevMeasure.ThreadCount)
            {
                GoToState("Thread count decreased externally", ExecutionThroughoutTrackerUpSmartDownCorrectionState.TryDecrease);
                return ReturnSuggestion("Thread count decreased externally", 0);
            }
            if (curMeasure.ThreadCount >= _maxThreadCount)
            {
                GoToState("Upper bound reached", ExecutionThroughoutTrackerUpSmartDownCorrectionState.InOptimalState);
                return ReturnSuggestion("Upper bound reached", 0);
            }

            Debug.Assert(curMeasure.ThreadCount > prevMeasure.ThreadCount);

            double throughoutDiffCoef = EstimateThroughoutDiffCoef(prevMeasure.ThreadCount, prevMeasure.Throughout, curMeasure.ThreadCount, curMeasure.Throughout);
            double avgThroughoutDiffCoef = EstimateThroughoutDiffCoef(prevMeasure.ThreadCount, prevMeasure.AverageThroughout, curMeasure.ThreadCount, curMeasure.AverageThroughout);

            //Console.WriteLine("throughout diff = " + throughoutDiffCoef.ToString());
            //Console.WriteLine("avgThroughout diff = " + avgThroughoutDiffCoef.ToString());

            if (throughoutDiffCoef < IncreaseDirectionLocDropDiff && avgThroughoutDiffCoef < IncreaseDirectionAvgDropDiff)
            {
                GoToState("Throughout drop", ExecutionThroughoutTrackerUpSmartDownCorrectionState.TryDecrease);
                return ReturnSuggestion("Throughout drop", 0);
            }

            if (throughoutDiffCoef < IncreaseDirectionLocGoodDiff && avgThroughoutDiffCoef < IncreaseDirectionAvgGoodDiff)
            {
                GoToState("Throughout increase slightly", ExecutionThroughoutTrackerUpSmartDownCorrectionState.InOptimalState);
                return ReturnSuggestion("Throughout increase slightly", 0);
            }

            return ReturnSuggestion("Continue thread increase", 1);
        }

        /// <summary>
        /// Сделать предложение по изменению числа потоков в TryDecreaseState
        /// </summary>
        /// <returns>Предложение</returns>
        private int MakeSuggestionInTryDecreaseState()
        {
            var curMeasure = GetCurrentMeasure();
            var prevMeasure = GetPrevMeasure();

            if (_stateMeasureCount == 1)
            {
                _tryDecreaseInitialThreadCount = curMeasure.ThreadCount;
            }
            Debug.Assert(_tryDecreaseInitialThreadCount > 0);

            if (_tryDecreaseBlinkCount != EstimationDataLength)
            {
                _tryDecreaseBlinkCount++;
                if (((_tryDecreaseBlinkCount - 1) % 2) == 0)
                    return ReturnSuggestion("TryDecrease blinking test up", _tryDecreaseInitialThreadCount - curMeasure.ThreadCount);
                else
                    return ReturnSuggestion("TryDecrease blinking test down", _reasonableThreadCount - curMeasure.ThreadCount);
            }


            double avgBaseThroughout = CalcAverageThroughoutForSomeElements(_data, _nextDataIndex, 1, 2);
            double avgPeekThroughout = CalcAverageThroughoutForSomeElements(_data, _nextDataIndex, 0, 2);
            int currentActiveThreadCountForDecreasing = Math.Max(_reasonableThreadCount, (int)Math.Floor(_reasonableThreadCount * (avgPeekThroughout / avgBaseThroughout)));

            if (currentActiveThreadCountForDecreasing >= curMeasure.ThreadCount - 1)
            {
                if (curMeasure.ThreadCount >= _maxThreadCount)
                {
                    GoToState("Better increase, but upper bound", ExecutionThroughoutTrackerUpSmartDownCorrectionState.InOptimalState);
                    return ReturnSuggestion("Better increase, but upper bound", 0);
                }
                else
                {
                    GoToState("Better increase", ExecutionThroughoutTrackerUpSmartDownCorrectionState.TryIncrease);
                    return ReturnSuggestion("Better increase", 1);
                }
            }

            GoToState("Decrease starts from " + currentActiveThreadCountForDecreasing.ToString(), ExecutionThroughoutTrackerUpSmartDownCorrectionState.Decreasing);
            return ReturnSuggestion("Decrease starts from " + currentActiveThreadCountForDecreasing.ToString(), currentActiveThreadCountForDecreasing - curMeasure.ThreadCount);
        }


        /// <summary>
        /// Сделать предложение по изменению числа потоков в DecreasingState
        /// </summary>
        /// <returns>Предложение</returns>
        private int MakeSuggestionInDecreasingState()
        {
            var curMeasure = GetCurrentMeasure();
            var prevMeasure = GetPrevMeasure();

            if (_stateMeasureCount <= 1)
            {
                return ReturnSuggestion("Skip first measure", 0);
            }

            if (curMeasure.ThreadCount <= prevMeasure.ThreadCount)
            {
                GoToState("Thread count decreased externally", ExecutionThroughoutTrackerUpSmartDownCorrectionState.TryDecrease);
                return ReturnSuggestion("Thread count decreased externally", 0);
            }
            if (curMeasure.ThreadCount >= _maxThreadCount)
            {
                GoToState("Upper bound reached", ExecutionThroughoutTrackerUpSmartDownCorrectionState.InOptimalState);
                return ReturnSuggestion("Upper bound reached", 0);
            }

            Debug.Assert(curMeasure.ThreadCount > prevMeasure.ThreadCount);

            double throughoutDiffCoef = EstimateThroughoutDiffCoef(prevMeasure.ThreadCount, prevMeasure.Throughout, curMeasure.ThreadCount, curMeasure.Throughout);
            double avgThroughoutDiffCoef = EstimateThroughoutDiffCoef(prevMeasure.ThreadCount, prevMeasure.AverageThroughout, curMeasure.ThreadCount, curMeasure.AverageThroughout);

            //Console.WriteLine("throughout diff = " + throughoutDiffCoef.ToString());
            //Console.WriteLine("avgThroughout diff = " + avgThroughoutDiffCoef.ToString());


            if (throughoutDiffCoef < IncreaseDirectionLocDropDiff && avgThroughoutDiffCoef < IncreaseDirectionAvgDropDiff)
            {
                GoToState("Throughout drop", ExecutionThroughoutTrackerUpSmartDownCorrectionState.TryDecrease);
                return ReturnSuggestion("Throughout drop", 0);
            }

            if (throughoutDiffCoef < IncreaseDirectionLocGoodDiff && avgThroughoutDiffCoef < IncreaseDirectionAvgGoodDiff)
            {
                GoToState("Throughout increase slightly", ExecutionThroughoutTrackerUpSmartDownCorrectionState.InOptimalState);
                return ReturnSuggestion("Throughout increase slightly", 0);
            }

            return ReturnSuggestion("Continue thread increase", 1);
        }


        /// <summary>
        /// Сделать предложение по изменению числа потоков
        /// </summary>
        /// <param name="needAction">Нужно ли что-то делать</param>
        /// <param name="isCriticalCondition">Критические ли условия</param>
        /// <returns>Предложение</returns>
        private int MakeSuggestion(bool needAction, bool isCriticalCondition)
        {
            if (!needAction)
            {
                int resultThreadCountDiff = DisablePerfMeasureThreadIfRequired();
                GoToState("Action not required", ExecutionThroughoutTrackerUpSmartDownCorrectionState.InOptimalState);
                return ReturnSuggestion("Action not required", resultThreadCountDiff);
            }

            if (isCriticalCondition)
            {
                if (_state != ExecutionThroughoutTrackerUpSmartDownCorrectionState.TryIncrease && _state != ExecutionThroughoutTrackerUpSmartDownCorrectionState.Increasing)
                    GoToState("Critical condition", ExecutionThroughoutTrackerUpSmartDownCorrectionState.TryIncrease);

                return ReturnSuggestion("Critical condition", 0);
            }

            switch (_state)
            {
                case ExecutionThroughoutTrackerUpSmartDownCorrectionState.InOptimalState:
                    return MakeSuggestionInOptimalState();
                case ExecutionThroughoutTrackerUpSmartDownCorrectionState.TryIncrease:
                    return MakeSuggestionInTryIncreaseState();
                case ExecutionThroughoutTrackerUpSmartDownCorrectionState.Increasing:
                    return MakeSuggestionInIncreasingState();
                case ExecutionThroughoutTrackerUpSmartDownCorrectionState.TryDecrease:
                    return MakeSuggestionInTryDecreaseState();
                case ExecutionThroughoutTrackerUpSmartDownCorrectionState.Decreasing:
                    return MakeSuggestionInDecreasingState();
                default:
                    throw new InvalidOperationException("Unknown state: " + _state.ToString());
            }
        }


        /// <summary>
        /// Зарегистрировать измерение и сделать предположение по улучшению производительности
        /// </summary>
        /// <param name="workThreadCount">Текущее число потоков</param>
        /// <param name="needAction">Нужно ли что-то делать</param>
        /// <param name="isCriticalCondition">Критические ли условия</param>
        /// <returns>На сколько надо изменить число потоков</returns>
        public int RegisterAndMakeSuggestion(int workThreadCount, bool needAction, bool isCriticalCondition)
        {
            Contract.Requires(workThreadCount >= 0);

            RegisterMeasure(workThreadCount, _isPerfMeasureThreadWork);
            return MakeSuggestion(needAction, isCriticalCondition);
        }

        public int RegisterAndMakeSuggestionTest(int workThreadCount, int executedTaskCount, int elapsedMs)
        {
            Contract.Requires(workThreadCount >= 0);
            RegisterExecution(executedTaskCount);
            RegisterMeasure(workThreadCount, _isPerfMeasureThreadWork, elapsedMs);
            return MakeSuggestion(true, false);
        }
    }
}
