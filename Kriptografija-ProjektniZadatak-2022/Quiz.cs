using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Security.Cryptography;
using System.Diagnostics;
//using System.Drawing.Imaging;

namespace Crypto
{
    public class Quiz
    {
        public static void Start(User user)
        {
            var input = "0";
            while ("3" != input)
            {
                System.Console.Clear();
                System.Console.WriteLine("[1] Start Quiz \n[2] View Results\n[3] Exit\n");
                System.Console.Write("Enter the choice: ");
                input = Console.ReadLine();

                if ("1" == input.Trim())
                {
                    if (user.NumberPlayed < 4)
                        GenerateQuestions(user);
                    else
                    {
                        Console.WriteLine("Ispunili ste 3 pokušaja igranja kviza.");
                        Thread.Sleep(5_000);
                        DigitalCertificate.RevokeCertificate(user);
                        // TODO: povlačenje sertifikata 
                    }
                }

                else if ("2" == input.Trim())
                    ViewQuizResults();
            }
            return;
        }

        /** 
         * bira 5 nasumičnih pitanja/slika
         */
        public static void GenerateQuestions(User user)
        {
            user.Score = 0; // jer za svaki novi kviz koji ulogovani korisnik igra, resetujemo trenutne bodove
            /* envelopa stegoKey.txt treba dekriptovati privatnim ključem CA1.key da bi se dobili parametri Aes ključa (key,iv) koji su se koristili za kriptovanje */
            string cypherParams = Utility.ExecuteShellCommand($"openssl rsautl -decrypt -inkey {Utility.CA1}\\private\\CA1.key -in {Utility.PRIVATE}\\stegoKey.txt");

            // string parsiramo i konvertujemo iz base64 u byte[]
            byte[] aesKey = Convert.FromBase64String(cypherParams.Split('#')[0]);
            byte[] aesIV = Convert.FromBase64String(cypherParams.Split('#')[1]);

            // lista mogućih brojeva 1-20 slika
            List<int> possible = Enumerable.Range(1, 20).ToList();
            // lista već odgovorenih pitanja
            List<int> done = new List<int> { 0 };
            Random rand = new Random();

            using (AesManaged myAes = new AesManaged())
            {
                myAes.Key = aesKey;
                myAes.IV = aesIV;
                // početak rada sa kvizom
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                for (int i = 0; i < 5; i++)
                {
                    int number;
                    do
                    {
                        number = rand.Next(1, possible.Count + 1);
                    } while (done.Contains(number));
                    done.Add(number);

                    //Console.WriteLine("Odabrani broj/pitanje " + number);

                    // učitava sliku iz foldera enc_pic
                    string encryptedQuestion = "";
                    using (var image = new Bitmap(Utility.ROOT + "\\enc_pics\\" + number + ".bmp"))
                    {
                        // izvlači pitanje kriptovano simetričnim algoritmom base64 kodovano
                        encryptedQuestion = Steganography.ExtractText(image);
                    }
                    byte[] questionBytes = Convert.FromBase64String(encryptedQuestion);
                    // Dekriptujemo pitanje
                    string decryptedQuestion = AES.DecryptStringFromBytes_Aes(questionBytes, myAes.Key, myAes.IV);

                    // kreirati pitanje konstruktorom
                    QuizQuestion question = new QuizQuestion(decryptedQuestion);
                    Console.WriteLine(question);
                    Console.Write("Odgovor: ");
                    string answer = Console.ReadLine();

                    if (question.CheckIfCorrect(answer))
                        user.Score++;

                    Console.WriteLine("===========================================================================");
                }
                stopwatch.Stop();
                Console.WriteLine("Ukupan rezultat: " + user.Score);
                // kad istekne svih 5 pitanja, upisati rezultat u fajl
                user.PrintResult(stopwatch);
                user.NumberPlayed++;
                user.UpdateUser();
                Thread.Sleep(7_000);

            }
        }

        /**
         * Dekriptujemo resultsKey.txt privatnim ključem CA1 tijela. (dobijemo parametre AES (key,iv))
         * Dekriptujemo rezultate AES algoritmom. 
         * Ispišemo korisniku formatirane rezultate.
         * */
        private static void ViewQuizResults()
        {

            /* envelopa resultsKey.txt treba dekriptovati privatnim ključem CA1.key da bi se dobili parametri Aes ključa (key,iv) koji su se koristili za kriptovanje */
            string cypherParams = Utility.ExecuteShellCommand($"openssl rsautl -decrypt -inkey {Utility.CA1}\\private\\CA1.key -in {Utility.PRIVATE}\\resultsKey.txt");

            // string parsiramo i konvertujemo iz base64 u byte[]
            byte[] aesKey = Convert.FromBase64String(cypherParams.Split('#')[0]);
            byte[] aesIV = Convert.FromBase64String(cypherParams.Split('#')[1]);

            using (AesManaged myAes = new AesManaged())
            {
                myAes.Key = aesKey;
                myAes.IV = aesIV;

                String filePath = Utility.RESULTS + Utility.SEPARATOR + "results.txt";
                // ako nema nijednog rezultata u fajlu
                if (new FileInfo(filePath).Length == 0)
                {
                    Console.WriteLine("Nema nijednog rezultata");
                    Thread.Sleep(3_000);
                    return;
                }
                else
                {
                    using (StreamReader reader = new StreamReader(filePath))
                    {
                        // base64 dekodujemo u byte[]
                        byte[] resultBytes = Convert.FromBase64String(reader.ReadToEnd());
                        // AES dekriptujemo sve rezultate
                        string decryptedResults = AES.DecryptStringFromBytes_Aes(resultBytes, myAes.Key, myAes.IV);
                        Console.WriteLine(decryptedResults);
                        Thread.Sleep(10_000);
                    }
                }
            }
        }
    }
}
