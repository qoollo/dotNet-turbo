using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.IoC.Injections
{
    /// <summary>
    /// Источник инъекций
    /// </summary>
    /// <typeparam name="TKey">Тип ключа для извлечения инъекций</typeparam>
    public interface IInjectionSource<in TKey>
    {
        /// <summary>
        /// Получение инъекции по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Инъекция</returns>
        object GetInjection(TKey key);

        /// <summary>
        /// Пытается получить инъекцию по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Значение, если найдено</param>
        /// <returns>Удалось ли получить значение</returns>
        bool TryGetInjection(TKey key, out object val);

        /// <summary>
        /// Содержит ли контейнер инъекцию
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Есть или нет</returns>
        bool Contains(TKey key);
    }
}
