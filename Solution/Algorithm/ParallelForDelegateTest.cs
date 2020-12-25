using System;
using System.Linq;
using Alea;
using Alea.Parallel;
using NUnit.Framework;

namespace Algorithm
{
    class ParallelForDelegateTest
    {
        private const int Length = 1000000;

        [GpuManaged]
        public static void Transform<T>(Func<T, T, T> op, T[] result, T[] arg1, T[] arg2)
        {
            Action<int> action = i => result[i] = op(arg1[i], arg2[i]);

            Gpu.Default.For(0, result.Length, action);
        }

        private static T ConvertValue<T, TU>(TU value) where TU : IConvertible
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }

        [Test]
        [GpuManaged]
        public static void AddDouble()
        {
            var arg1 = Enumerable.Range(0, Length).Select(ConvertValue<double, int>).ToArray();
            var arg2 = Enumerable.Range(0, Length).Select(ConvertValue<double, int>).ToArray();
            var result = new double[Length];

            Transform((x, y) => x + y, result, arg1, arg2);

            var expected = arg1.Zip(arg2, (x, y) => x + y);

            Assert.That(result, Is.EqualTo(expected));
        }

        private struct Complex<T>
        {
            public T Real;
            public T Imag;

            public override string ToString()
            {
                return $"({Real}+I{Imag})";
            }
        }

        private static MagicInput Test(MagicInput a, int[] b)
        {
            var t = a.X;
            while (t >> 56 != 0)
            {
                t = t >> 10;
            }
            return new MagicInput {X = a.X};
        }

        [GpuManaged, Test]
        public static void AddComplexDouble()
        {
            var rng = new Random();
            var arg1 = Enumerable.Range(0, Length).Select(i => new MagicInput { X = (ulong)rng.Next(), X0 = (ulong)rng.Next() }).ToArray();
            var arg2 = Enumerable.Range(0, Length).Select(i =>  new MagicInput { X = (ulong)rng.Next(), X0 = (ulong)rng.Next() }).ToArray();
            var result = new MagicInput[Length];

            Func<MagicInput, MagicInput, MagicInput> complexAdd = (x, y) =>
            {
                var t = x.X;
                while (t >> 56 != 0)
                {
                    if (t != 0)
                    {
                        return new MagicInput {X = x.X};
                    }
                }
                return new MagicInput {X = x.X};
            };

            var s = new int[16];
            
            Gpu.Default.For(0, result.Length, i => { result[i] = Test(arg1[i], s); });
            
//           Transform(complexAdd, result, arg1, arg2);

            var expected = arg1.Zip(arg2, complexAdd);

            Assert.That(result, Is.EqualTo(expected));
        }     
    }

}