﻿using System;
using GSF.IO.Test;
using NUnit.Framework;

namespace GSF.IO.Unmanaged.Test
{
    [TestFixture()]
    public class UnmanagedMemoryStreamTest
    {
        [Test()]
        public void Test()
        {
            MemoryPoolTest.TestMemoryLeak();
            SelfTest();
            UnmanagedMemoryStream ms = new UnmanagedMemoryStream();
            BinaryStreamTest.Test(ms);
            Assert.IsTrue(true);
            ms.Dispose();
            MemoryPoolTest.TestMemoryLeak();
        }

        private static void SelfTest()
        {
            UnmanagedMemoryStream ms1 = new UnmanagedMemoryStream();
            BinaryStreamBase ms = ms1.CreateBinaryStream();
            Random rand = new Random();
            int seed = rand.Next();
            rand = new Random(seed);
            byte[] data = new byte[255];
            rand.NextBytes(data);

            while (ms.Position < 1000000)
            {
                ms.Write(data, 0, rand.Next(256));
            }

            byte[] data2 = new byte[255];
            rand = new Random(seed);
            rand.NextBytes(data2);
            ms.Position = 0;
            Compare(data, data2, 255);
            while (ms.Position < 1000000)
            {
                int length = rand.Next(256);
                ms.ReadAll(data2, 0, length);
                Compare(data, data2, length);
            }
            ms.Dispose();
            ms1.Dispose();
        }

        private static void Compare(byte[] a, byte[] b, int length)
        {
            for (int x = 0; x < length; x++)
            {
                if (a[x] != b[x])
                    throw new Exception();
            }
        }
    }
}