using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using System.IO;
using Seq2SeqLearn;


namespace Chatbot
{
    class Program
    {
        static AttentionSeq2Seq S2S;
        static Thread MainThread;
        static Thread ReadThread;
        static List<List<string>> input = new List<List<string>>();
        static List<List<string>> output = new List<List<string>>();

        static void Main(string[] args)
        {
            System.Random r = new Random(5);
            Preprocess();    
            S2S = new AttentionSeq2Seq(32, 16, 1, input, output, true);
            try { S2S.Load(); } catch (Exception) { }

            int c = 0;
            S2S.IterationDone += (a1, a2)=> 
            {
                CostEventArg ep = a2 as CostEventArg;                

                if (c % 100 == 0)
                {
                    Console.WriteLine($"Cost {ep.Cost} Iteration {ep.Iteration} k {c}");
                    S2S.Save();
                }
                c++;
            };

            MainThread = new Thread(new ThreadStart(Train));
            MainThread.Start();

            ReadThread = new Thread(new ThreadStart(ReadingConsole));
            ReadThread.Start();
        }
        static void Preprocess()
        {
            var HumanTextRaw = File.ReadAllLines("human_text.txt");
            var RobotTextRaw = File.ReadAllLines("robot_text.txt");

            for (int i = 0; i < HumanTextRaw.Length; i++)
            {
                HumanTextRaw[i] = RemoveAccentMark(HumanTextRaw[i]);

                RobotTextRaw[i] = RemoveAccentMark(RobotTextRaw[i]);
            }

            var HumanText = new List<string>();
            var RobotText = new List<string>();
            for (int i = 0; i < 1000; i++)
            {
                HumanText.Add(HumanTextRaw[i]);
                RobotText.Add(RobotTextRaw[i]);
            }

            for (int i = 0; i < HumanText.Count; i++)
            {
                input.Add(HumanText[i].ToLower().Trim().Split(' ').ToList());
                output.Add(RobotText[i].ToLower().Trim().Split(' ').ToList());
            }
        }
        static void Train()
        {
            S2S.Train(300);
        }
        static void ReadingConsole()
        {
            while (true)
            {
                var Line = RemoveAccentMark(Console.ReadLine().ToLower());
                if (!MainThread.IsAlive)
                {
                    if (Line == "!resume")
                    {
                        MainThread = new Thread(new ThreadStart(Train));
                        MainThread.Start();
                    }
                    else
                    {
                        try
                        {
                            var pred = S2S.Predict(Line.Trim().Split(' ').ToList());
                            Console.WriteLine($"<< {Line}");

                            Console.Write(">>");
                            for (int i = 0; i < pred.Count; i++)
                            {
                                Console.Write(pred[i] + " ");
                            }
                            Console.WriteLine();
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("Deja de usar palabras raras crj");
                        }
                    }
                }
                else
                {
                    if (Line == "!stop")
                    {
                        MainThread.Abort();
                        Console.WriteLine("Process stoped");
                        S2S.Save();
                    }
                }
            }
        }
        static string RemoveAccentMark(string i)
        {
            return i.Replace('á', 'a')
                    .Replace('é', 'e')
                    .Replace('í', 'i')
                    .Replace('ó', 'o')
                    .Replace('ú', 'u')
                    .Replace('Á', 'A')
                    .Replace('É', 'E')
                    .Replace('Í', 'I')
                    .Replace('Ó', 'O')
                    .Replace('Ú', 'U');
        }
    }
}
