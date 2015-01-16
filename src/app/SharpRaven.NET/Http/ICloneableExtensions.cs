using System;

namespace SharpRaven.Http
{
    /// <summary>
    /// Extension methods for <see cref="ICloneable"/> objects.
    /// </summary>

    static class ICloneableExtensions
    {
        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>

        public static T CloneObject<T>(this T source) where T : class, ICloneable
        {
            if (source == null) throw new ArgumentNullException("source");
            return (T)source.Clone();
        }
    }
}