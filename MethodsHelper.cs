using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDLTTFSharp_FNA
{
    public static class MethodsHelper
    {
        /// <summary>
        /// 从指定的集合中查找指定的项
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="list">集合</param>
        /// <param name="match">查找规则</param>
        /// <param name="result">返回结果</param>
        /// <returns>如果查找成功，返回true，否则返回false</returns>
        public static bool TryFind<T>(this IEnumerable<T> list, Predicate<T> match, out T result)
        {
            foreach (var item in list)
            {
                if (match(item))
                {
                    result = item;
                    return true;
                }
            }
            result = default;
            return false;
        }
    }
}
