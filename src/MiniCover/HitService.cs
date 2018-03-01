﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace MiniCover
{
    public static class HitService
    {
        private static readonly object lockObject = new object();
        private static Dictionary<string, Dictionary<int, HitTrace>> files = new Dictionary<string, Dictionary<int, HitTrace>>();

        public static void Init(string fileName)
        {
            lock (lockObject)
            {

                if (!files.ContainsKey(fileName))
                {
                    files[fileName] = new Dictionary<int, HitTrace>();
                }
            }
        }

        public static void Hit(string fileName, int id)
        {
            lock (lockObject)
            {
                var hits = files[fileName];
                if (!hits.ContainsKey(id))
                    hits.Add(id, new HitTrace(id));
                hits[id].Hited();
            }
        }

        static void Save()
        {
            foreach (var fileName in files)
            {
                using (var streamWriter = new StreamWriter(File.Open(fileName.Key, FileMode.Append)))
                {
                    foreach (var hitTrace in fileName.Value)
                    {
                        hitTrace.Value.WriteInformation(streamWriter);
                    }

                    streamWriter.Flush();
                }
            }

        }

        static HitService()
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, args) => Save();
        }

        private class HitTrace
        {
            private readonly int integrationId;

            private int hits;

            public HitTrace(int integrationId)
            {
                this.integrationId = integrationId;
            }

            internal void Hited()
            {
                hits++;
            }

            internal void WriteInformation(StreamWriter writer)
            {
                writer.WriteLine(
                    $"{integrationId}:{hits}");
            }
        }
    }
}
