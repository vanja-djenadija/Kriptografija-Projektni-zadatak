using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Serialization.Formatters.Binary;

namespace Crypto
{

    public class Utility
    {
        public static readonly string SEPARATOR = Path.DirectorySeparatorChar.ToString();//   
        public static readonly string ROOT = "..\\..\\..\\.";//   TODO: NE APSOLUTNA PUTANJA
        public static readonly string RESOURCES_DIR = ROOT + SEPARATOR + "resources"; //
        public static readonly string RESULTS = RESOURCES_DIR + SEPARATOR + "results";                                                                          //  
        public static readonly string CERTIFICATES_ROOT = RESOURCES_DIR + SEPARATOR + "certificates";//  
        public static readonly string CERTS = CERTIFICATES_ROOT + SEPARATOR + "certs";
        public static readonly string CRL = CERTIFICATES_ROOT + SEPARATOR + "crl";
        public static readonly string NEWCERTS = CERTIFICATES_ROOT + SEPARATOR + "newcerts";
        public static readonly string PRIVATE = CERTIFICATES_ROOT + SEPARATOR + "private";
        public static readonly string REQUESTS = CERTIFICATES_ROOT + SEPARATOR + "requests";
        public static readonly string CA1 = CERTIFICATES_ROOT + SEPARATOR + "CA1";
        public static readonly string CA2 = CERTIFICATES_ROOT + SEPARATOR + "CA2";
        public static readonly string USERS = CERTIFICATES_ROOT + "\\users.txt";

        public static void PrepareEnvironment()
        {

            Directory.CreateDirectory(RESOURCES_DIR);
            Directory.CreateDirectory(RESULTS);
            Directory.CreateDirectory(CERTIFICATES_ROOT);
            Directory.CreateDirectory(CERTS);
            Directory.CreateDirectory(CRL);
            Directory.CreateDirectory(NEWCERTS);
            Directory.CreateDirectory(PRIVATE);
            Directory.CreateDirectory(REQUESTS);

            if (!File.Exists(CERTIFICATES_ROOT + SEPARATOR + "index.txt"))
            {
                var file = File.Create(CERTIFICATES_ROOT + SEPARATOR + "index.txt");
                file.Close();
            }

            if (!File.Exists(CERTIFICATES_ROOT + SEPARATOR + "serial"))
                File.WriteAllText(CERTIFICATES_ROOT + SEPARATOR + "serial", "01");


            if (!File.Exists(CERTIFICATES_ROOT + SEPARATOR + "crlnumber"))
                File.WriteAllText(CERTIFICATES_ROOT + SEPARATOR + "crlnumber", "01");

            if (!Directory.Exists(CA1))
                DirectoryCopy(CERTIFICATES_ROOT, CA1, true);

            if (!Directory.Exists(CA2))
                DirectoryCopy(CERTIFICATES_ROOT, CA2, true);

            // placing .cnf files in folders of CAs  
            PlaceConfigFile(CERTIFICATES_ROOT, "rootCA.cnf");
            PlaceConfigFile(CA1, "CA1.cnf");
            PlaceConfigFile(CA2, "CA2.cnf");

            // creating CA certificates 
            if (!File.Exists(CERTIFICATES_ROOT + SEPARATOR + "rootCA.pem"))
                DigitalCertificate.CreateCACertificate();

            if (!File.Exists(CA1 + SEPARATOR + "CA1.pem"))
                DigitalCertificate.CreateIntermediateCACertificate("CA1", CA1, 180, 4096);

            if (!File.Exists(CA2 + SEPARATOR + "CA2.pem"))
                DigitalCertificate.CreateIntermediateCACertificate("CA2", CA2, 180, 4096);

            if (!File.Exists(USERS))
            {
                var usersFile = File.Create(USERS);
                usersFile.Close();
            }

            //TODO: generisanje AES simetričnog ključa, i pravljenje envelope od njega


            // Kreiranje parametara simetričnog ključa koji se koriste za kriptovanje rezultata
            if (!File.Exists(PRIVATE + SEPARATOR + "resultsKey.txt"))
                CreateResultsKey();

            if (!File.Exists(RESULTS + SEPARATOR + "results.txt"))
            {
                var file = File.Create(RESULTS + SEPARATOR + "results.txt");
                file.Close();
            }

        }

        private static void PlaceConfigFile(string path, string fileName)
        {
            bool print = true;
            while (!File.Exists(Path.Combine(path, fileName)))
            {
                if (print)
                {
                    System.Console.WriteLine($"Please place {fileName} in {path}");
                    print = false;
                }
            }
            Console.Clear();
        }

        /** Source: https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories */
        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    // kopiraj samo one direktorijume koji nisu namijenjeni za CA podređena tijela -> naziv oblika: CA_n
                    if (!subdir.FullName.Contains("CA"))
                    {
                        string tempPath = Path.Combine(destDirName, subdir.Name);
                        DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                    }
                }
            }
        }

        public static string ExecuteShellCommand(string command)
        {
            Process process = new Process();
            process.StartInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
            process.StartInfo.WorkingDirectory = CERTIFICATES_ROOT;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.Start();
            while (!process.HasExited) ;
            return process.StandardOutput.ReadToEnd().Trim();
        }

        /* 
         * učitavamo fajl sa 20 pitanja
         * za svako pitanje/red u fajlu kriptujemo istim AES algoritmom
        */
        public static void HideQuestionsInImages()
        {
            string pathToQuestions = ROOT + "\\pitanja.txt";
            string[] questions = File.ReadAllLines(ROOT + "\\pitanja.txt"); // garantuje se da će fajl sa pitanjima postojati na toj lokaciji
            int number = 1;
            using (AesManaged myAes = new AesManaged())
            {
                foreach (string question in questions)
                {
                    // Kriptujemo pitanje AES algoritmom u niz bajtova.
                    byte[] encryptedQuestionBytes = AES.EncryptStringToBytes_Aes(question, myAes.Key, myAes.IV);
                    // Otvori sliku pod odgovarajućim rednim brojem
                    Bitmap newImage = null;
                    using (var image = new Bitmap(ROOT + "\\pics\\" + number + ".bmp"))
                    {
                        newImage = new Bitmap(image);
                    }
                    // konvertujemo niz bajtova u base64 string
                    string encryptedQuestionString = Convert.ToBase64String(encryptedQuestionBytes);
                    Steganography.EmbedText(encryptedQuestionString, newImage); // ubacujemo kriptovano pitanje u sliku 
                    Directory.CreateDirectory(ROOT + "\\enc_pics"); // TODO: Ovo prebaciti u PrepareEnvironment
                    newImage.Save($"{ROOT}\\enc_pics\\{number}.bmp", ImageFormat.Bmp);
                    newImage.Dispose();
                    number++;
                }
                // pretvorimo Key i IV AES algoritma u BASE64 string
                string aesKey = Convert.ToBase64String(myAes.Key);
                string iv = Convert.ToBase64String(myAes.IV);
                string symmetricCipher = aesKey + "#" + iv;
                //Console.WriteLine(symmetricCipher);
                // parametre AES ključa u formi "kljuc#iv" kriptujemo javnim ključem CA1 tijela
                ExecuteShellCommand($"echo {symmetricCipher} | openssl rsautl -encrypt -out {PRIVATE}\\stegoKey.txt -pubin -inkey {CA1}\\private\\PUB_CA1.key");
            }
        }

        // TODO: Da li treba ključ kriptovati javnim ključem CA1 tijela
        private static void CreateResultsKey()
        {
            string key;
            string iv;
            using (AesManaged myAes = new AesManaged())
            {
                key = Convert.ToBase64String(myAes.Key);
                iv = Convert.ToBase64String(myAes.IV);
                string symmetricCipher = key + "#" + iv;
                ExecuteShellCommand($"echo {symmetricCipher} | openssl rsautl -encrypt -out {PRIVATE}\\resultsKey.txt -pubin -inkey {CA1}\\private\\PUB_CA1.key");
                //Console.WriteLine("Simetrični ključ za rezultate: " + symmetricCipher);
                Thread.Sleep(5000);
            }
        }
    }
}