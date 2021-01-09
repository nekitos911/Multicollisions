namespace MoraHash
{
    public static class Constants
    {
        public static readonly ulong[] C =
        {
            0xc0164633575a9699,
            0x925b4ef49a5e7174,
            0x86a89cdcf673be26,
            0x1885558f0eaca3f1,
            0xdcfc5b89e35e8439,
            0x54b9edc789464d23,
            0xf80d49afde044bf9,
            0x8cbbdf71ccaa43f1,
            0xcb43af722cb520b9
        };

        // Нелинейное биективное преобразование множества двоичных векторов
        public static readonly int[] SBox =
        {
            15, 9, 1, 7, 13, 12, 2, 8, 6, 5, 14, 3, 0, 11, 4, 10
        };
        
        // Перестановка полубайт.
        public static readonly int[] Tau =
        {
            0, 4, 8, 12, 1, 5, 9, 13, 2, 6, 10, 14, 3, 7, 11, 15
        };

        // Линейное преобразование множества двоичных векторов.
        public static readonly int[] L =
        {
            0x3a22, 0x8511, 0x4b99, 0x2cdd,
            0x483b, 0x248c, 0x1246, 0x9123,
            0x59e5, 0xbd7b, 0xcfac, 0x6e56,
            0xac52, 0x56b1, 0xb3c9, 0xc86d
        };
    }
}