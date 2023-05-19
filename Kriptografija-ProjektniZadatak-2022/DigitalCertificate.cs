using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Crypto
{
    public class DigitalCertificate
    {
        public static void CreateCACertificate()
        {
            System.Console.Clear();
            System.Console.WriteLine("[ Kreiranje root CA tijela : rootCA]\n");
            Utility.ExecuteShellCommand($"openssl genrsa -out {Utility.PRIVATE}\\private4096.key 4096 2>error");
            Utility.ExecuteShellCommand($"openssl req -x509 -new -out rootCA.pem -config rootCA.cnf -days 365 -key {Utility.PRIVATE}\\private4096.key");
            System.Console.Clear();
        }

        public static void CreateIntermediateCACertificate(string name, string path, int days, int keylength = 4096)
        {
            System.Console.WriteLine($"\n[ Kreiranje podređenog CA tijela : {name}]\n");
            Utility.ExecuteShellCommand($"openssl genrsa -out {path}\\private\\{name}.key {keylength}");
            Utility.ExecuteShellCommand($"openssl rsa -in {path}\\private\\{name}.key -inform PEM -pubout -out {path}\\private\\PUB_{name}.key"); // TODO: Provjeriti da li funkcioniše
            Utility.ExecuteShellCommand($"openssl req -new -out requests\\{name}.csr -key {path}\\private\\{name}.key -config {Utility.CERTIFICATES_ROOT}\\rootCA.cnf -days {days}");
            Utility.ExecuteShellCommand($"openssl ca -config rootCA.cnf -in requests\\{name}.csr -out {path}\\{name}.pem -days {days}");
            System.Console.Clear();
        }

        public static void CreateUserCertificate(string username, string password, string caFolder, string caType)
        {
            System.Console.WriteLine($"[ Kreiranje korisničkog sertifikata : {username}]\n");
            Directory.CreateDirectory(caFolder + "\\userKeys");
            Utility.ExecuteShellCommand($"openssl genrsa -des3 -passout pass:{password} -out {caFolder}\\userKeys\\{username}.key 4096");
            Utility.ExecuteShellCommand($"openssl req -new -key {caFolder}\\userKeys\\{username}.key -passin pass:{password} -config {caFolder}\\{caType}.cnf -out {caFolder}\\requests\\{username}.csr");
            Utility.ExecuteShellCommand($"openssl ca -in {caFolder}\\requests\\{username}.csr -out {caFolder}\\certs\\{username}.crt -keyfile {caFolder}\\private\\{caType}.key -config {caFolder}\\{caType}.cnf");
            Thread.Sleep(10_000);
            System.Console.Clear();
        }

        // TODO: 
        public static bool VerifyCertificate(string username)
        {
            if (!File.Exists("crl\\crl-list.pem"))
            {
                System.Console.WriteLine("\nCreating CRL list.");
                Utility.ExecuteShellCommand($"openssl ca -gencrl -out crl\\crl-list.pem -config openssl.cnf");
            }

            var valid = (Utility.ExecuteShellCommand($"openssl verify -crl_check -CAfile rootca.pem -CRLfile crl\\crl-list.pem {Utility.CERTIFICATES_ROOT}\\{username}.crt")).Contains("OK");
            //Directory.SetCurrentDirectory($"{Utility.ROOT_FOLDER}");
            return valid;
        }

        // TODO: 
        public static void RevokeCertificate(User user)
        {
            //Directory.SetCurrentDirectory(Utility.CERTIFICATES_ROOT);
            Console.WriteLine("Trenutni radni direktorijum: " + Directory.GetCurrentDirectory());
            if (File.Exists(Utility.CA1 + "\\certs\\" + user.Username + ".crt")) // CA1
            {
                Console.WriteLine("Sertifikat se nalazi u CA1");
                Utility.ExecuteShellCommand($"openssl ca –revoke CA1\\certs\\{user.Username}.crt –crl_reason cessationOfOperation –config CA1\\CA1.cnf");
                string crlPath = "C:\\Users\\Administrator\\Desktop\\Kriptografija-ProjektniZadatak-2022\\Kriptografija-ProjektniZadatak-2022\\resources\\certificates\\CA1\\crl\\crl.pem";
                Utility.ExecuteShellCommand($"openssl ca –gencrl –out CA1\\crl.pem -config CA1\\CA1.cnf");
                Thread.Sleep(6_000);
            }
            else
            {  // CA2

                Console.WriteLine("Sertifikat se nalazi u CA2");
                string crlPath = "C:\\Users\\Administrator\\Desktop\\Kriptografija-ProjektniZadatak-2022\\Kriptografija-ProjektniZadatak-2022\\resources\\certificates\\CA2\\crl\\crl.pem";
                //Utility.ExecuteShellCommand($"openssl ca –revoke {Utility.CA2}\\certs\\{user.Username}.crt –crl_reason cessationOfOperation –config {Utility.CA2}\\CA2.cnf");
                Utility.ExecuteShellCommand($"openssl ca –gencrl –out CA2\\crl.pem –config CA2\\CA2.cnf");
                Thread.Sleep(6_000);
            }
        }
    }
}
