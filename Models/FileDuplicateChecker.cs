using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace WPFTasks.Models
{
    public class FileDuplicateChecker
    {
        /// <summary>
        /// Проверяет, является ли файл дубликатом по сравнению с другим файлом, используя хеширование.
        /// </summary>
        /// <param name="filePath1">Путь к первому файлу.</param>
        /// <param name="filePath2">Путь ко второму файлу.</param>
        /// <returns>True, если файлы идентичны, иначе False.</returns>
        public static bool AreFilesDuplicates(string filePath1, string filePath2)
        {
            using var hashAlgorithm = SHA256.Create();
            var hash1 = GetFileHash(hashAlgorithm, filePath1);
            var hash2 = GetFileHash(hashAlgorithm, filePath2);

            return StructuralComparisons.StructuralEqualityComparer.Equals(hash1, hash2);
        }

        public static byte[] GetFileHash(HashAlgorithm hashAlgorithm, string filePath)
        {
            var bytes = File.ReadAllBytes(filePath);
            return hashAlgorithm.ComputeHash(bytes);
        }
    }

    public class OperationReport
    {
        public string FileName { get; set; }
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }
        public bool WasDuplicate { get; set; }
        public bool AlreadyExists { get; set; }
        public string OriginalFileName { get; set; } // Имя оригинала
        public string OriginalFilePath { get; set; } // Путь оригинала

        public override string ToString()
        {
            if (WasDuplicate)
            {
                return $"Дубликат файла: \"{OriginalFileName}\" в исходной директории: {SourcePath}";
            }
            else
            {
                return $"Файл \"{FileName}\" перемещён в: {DestinationPath}";
            }
        }
    }

    public class InfoOfFile
    {
        public string Extension { get; set; }
        public byte[] Hash { get; set; }
        public string SourcePath { get; set; }
        public string FileName { get; set; }
    }
}
