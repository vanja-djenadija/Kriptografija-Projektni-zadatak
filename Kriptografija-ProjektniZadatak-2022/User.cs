using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Diagnostics;
//using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Crypto
{
    public class User
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public int Score { get; set; } = 0;

        public int NumberPlayed { get; set; }

        public int UserChoice { get; set; }

        public static int Choice { get; set; } = 0;

        private static Dictionary<string, (string, string, int)> users = new Dictionary<string, (string, string, int)>();

        // kad se uloguje korisnik kreiramo njegov objekat
        public User(string username, string hashPassword, int numberPlayed = 0)
        {
            Username = username;
            Password = hashPassword;
            NumberPlayed = numberPlayed;
        }
        private static Dictionary<string, (string, string, int)> GetUsers()
        {
            users.Clear();
            foreach (var line in File.ReadLines(Utility.USERS))
            {
                var values = line.Split(';');
                users.Add(values[0], (values[1], values[2], Int32.Parse(values[3])));
            }
            return users;
        }

        public static User LogIn()
        {
            // provjera da li ima u bazi podataka korisnika
            // da li se unesena šifra poklapa sa onom u bazi podataka
            // odabir opcije da se radi kviz/pregled rezultata

            Console.Clear();
            Console.WriteLine("[LOGIN]\n");
            GetUsers();
            Console.Write("Enter username: ");
            var username = Console.ReadLine().Trim();

            Console.Write("Enter password: ");
            string password = ReadPassword();

            // TODO: Učitati i broj pokušaja ✔
            // nađemo u bazi red koji upisuje određenog korisnika, dobijemo hashPass i salt u userInfo
            users.TryGetValue(username, out (string hash, string salt, int numberPlayed) userInfo);

            if (!users.ContainsKey(username))
                System.Console.WriteLine("\nNalog ne postoji.\n");

            // provjera da li je lozinka unesena za dato korisničko ime ispravna
            else if (VerifyPassword(password, userInfo.hash, userInfo.salt))
            {
                // TODO: 
                // provjera validnosti sertifikata
                /*if (!DigitalCertificate.VerifyCertificate(username)) 
                {
                    System.Console.WriteLine("\nSertifikat nije validan.\n");
                    return null;
                }*/

                System.Console.WriteLine("\nUspješno ste se prijavili.\n");
                Thread.Sleep(5_000);

                // TODO: Učitati i broj pokušaja ✔
                return new User(username, userInfo.hash, userInfo.numberPlayed);
            }
            Thread.Sleep(5_000);
            return null;
        }

        public static void SignUp()
        {
            Console.Clear();
            Console.WriteLine("[SIGNUP]\n");
            Console.Write("Enter username: ");
            var username = Console.ReadLine().Trim();

            Console.Write("Enter password: ");
            string password = ReadPassword();

            // provjera da li korisnik već postoji u bazi podataka
            if (UserNameExists(username))
                Console.WriteLine("\nKorisnicko ime vec postoji.");

            // provjera dužine lozinke
            else if (username.Length == 0)
                Console.WriteLine("\nKorisnicko ime treba da ima minimalno 1 karakter.");

            // provjera dužine lozinke
            else if (password.Length < 8)
                Console.WriteLine("\nLozinka treba da ima minimalno 8 karaktera.");
            else
            {
                string caName = "";
                string caFolder = "";

                // CA1
                if (Choice == 0)
                {
                    caName = "CA1";
                    caFolder = Utility.CA1;
                    Choice = 1;
                } // CA2
                else
                {
                    caName = "CA2";
                    caFolder = Utility.CA2;
                    Choice = 0;
                }

                /* TODO: obrisati */
                System.Console.WriteLine($"\nCA chosen: {caName}\n");
                System.Threading.Thread.Sleep(2_000);

                DigitalCertificate.CreateUserCertificate(username, password, caFolder, caName);

                string certificatePath = Path.Combine(caFolder, "certs", username + ".crt");
                if (File.Exists(certificatePath))
                {
                    // upisati u bazu podataka username;hash;salt
                    var writer = new StreamWriter(Utility.USERS, append: true);
                    int salt = RandomNumberGenerator.GetInt32(Int32.MaxValue);
                    writer.WriteLine(username + ";" + HashPassword(password, salt.ToString()) + ";" + salt.ToString() + ";0", true); // TODO: Dodati broj pokušaja = 0
                    writer.Close();

                    PrintCertificateKeyPath(caFolder, username);
                }
                else
                    Console.WriteLine("\nNeuspješna registracija!\nPokušajte ponovo...\n");
            }
            System.Threading.Thread.Sleep(5_000);
            System.Console.Clear();
        }

        // Ažuriramo sadržaj users.txt fajla, odnosno posljednji atribut za korisnika NumberPlayed
        public void UpdateUser()
        {
            string[] users = File.ReadAllLines(Utility.USERS);
            string[] updatedUsers = new string[users.Length];
            int i = 0;
            foreach (string user in users)
            {
                if (user.Split(';')[0].Equals(Username))
                {
                    string withoutNumberPlayed = user.Substring(0, user.LastIndexOf(';'));
                    updatedUsers[i++] = withoutNumberPlayed + ";" + NumberPlayed;
                }
                else
                    updatedUsers[i++] = user;
            }
            // upišemo u fajl sve korisnike
            File.WriteAllLines(Utility.USERS, updatedUsers);
        }

        private static bool VerifyPassword(string password, string hashPass, string salt)
        {
            if (HashPassword(password, salt) == hashPass)
                return true;

            Console.WriteLine("\nLozinka nije ispravna.");
            return false;
        }
        private static void PrintCertificateKeyPath(string caFolder, string username)
        {
            // poruka o uspješnoj registraciji
            System.Console.WriteLine(" Uspješna registracija!");
            // ispisati korisniku putanju do sertifikata i njegov ključ
            System.Console.WriteLine("=========================== Putanja do sertifikata ==========================");
            System.Console.WriteLine($"{caFolder}\\certs\\{username}.crt\n");
            System.Console.WriteLine("=========================== Putanja do ključa ===============================");
            System.Console.WriteLine($"{caFolder}\\userKeys\\{username}.key\n");
            System.Threading.Thread.Sleep(5_000);
        }

        private static string HashPassword(string password, string salt)
        {
            return Utility.ExecuteShellCommand($"openssl passwd -6 -salt {salt} {password}").Trim();
        }

        private static bool UserNameExists(string username)
        {
            var users = GetUsers();
            if (users.ContainsKey(username))
                return true;
            return false;
        }

        // TODO: Limit length of a password
        private static string ReadPassword()
        {
            var pass = string.Empty;
            ConsoleKey key;
            do
            {
                var keyInfo = Console.ReadKey(intercept: true);
                key = keyInfo.Key;

                if (key == ConsoleKey.Backspace && pass.Length > 0)
                {
                    Console.Write("\b \b");
                    pass = pass[0..^1];
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    Console.Write("*");
                    pass += keyInfo.KeyChar;
                }
            } while (key != ConsoleKey.Enter);
            return pass;
        }

        // Upisujemo u fajl results//results.txt rezultat korisnika KORISNIČKO_IME VRIJEME REZULTAT
        public void PrintResult(Stopwatch stopwatch)
        {
            TimeSpan ts = stopwatch.Elapsed;
            string time = String.Format("Vrijeme {0:00}:{1:00}:{2:00}.{3}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
            string resultEntry = String.Format("{0} {1} Rezultat: {2}", this.Username, time, this.Score);

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
                if (new FileInfo(filePath).Length == 0)
                {
                    using (StreamWriter sw = new StreamWriter(filePath))
                    {
                        byte[] encryptedResultBytes = AES.EncryptStringToBytes_Aes(resultEntry, myAes.Key, myAes.IV);
                        // konvertujemo niz bajtova u base64 string
                        string encryptedResultString = Convert.ToBase64String(encryptedResultBytes);
                        sw.Write(encryptedResultString);
                    }
                }
                else
                {
                    string encryptedResultString = "";
                    using (StreamReader sr = new StreamReader(filePath))
                    {
                        // base64 dekodujemo u byte[]
                        byte[] resultBytes = Convert.FromBase64String(sr.ReadToEnd());
                        // AES dekriptujemo sve rezultate
                        string decryptedResults = AES.DecryptStringFromBytes_Aes(resultBytes, myAes.Key, myAes.IV);
                        // dodajemo novi resultEntry u novi red
                        decryptedResults += ("\n" + resultEntry);
                        // kriptujemo novi sadržaj fajla na standardan način (digitalna envelopa)
                        byte[] encryptedResultBytes = AES.EncryptStringToBytes_Aes(decryptedResults, myAes.Key, myAes.IV);
                        encryptedResultString = Convert.ToBase64String(encryptedResultBytes);
                    }
                    using (StreamWriter sw = new StreamWriter(filePath))
                    {
                        sw.Write(encryptedResultString);
                    }
                }
            }
        }
    }
}

