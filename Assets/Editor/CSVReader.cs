using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Text;
public class CSVReader
{
    // 读取CSV文件并返回字典列表
    public static List<Dictionary<string, string>> ReadCSV(string filePath)
    {
        var result = new List<Dictionary<string, string>>();

        if (!File.Exists(filePath))
        {
            Debug.LogError($"文件不存在: {filePath}");
            return result;
        }

        using (var reader = new StreamReader(filePath))
        {
            // 读取表头 
            reader.ReadLine(); // 跳过第一行
            string headerLine = ReadCSVLine(reader); // 读取第二行作为表头
            if (string.IsNullOrEmpty(headerLine))
                return result;

            string[] headers = ParseCSVLine(headerLine);

            // 读取数据行
            while (!reader.EndOfStream)
            {
                string line = ReadCSVLine(reader);
                if (string.IsNullOrEmpty(line))
                    continue;

                string[] values = ParseCSVLine(line);

                var dict = new Dictionary<string, string>();
                for (int i = 0; i < headers.Length && i < values.Length; i++)
                {
                    dict[headers[i]] = values[i];
                }
                result.Add(dict);
            }
        }

        return result;
    }

        // 读取完整的CSV行（支持字段内的换行符）
    static string ReadCSVLine(StreamReader reader)
    {
        StringBuilder lineBuilder = new StringBuilder();
        bool inQuotes = false;
        bool lineComplete = false;
        
        while (!reader.EndOfStream && !lineComplete)
        {
            string currentLine = reader.ReadLine();
            if (currentLine == null)
                break;
                
            if (lineBuilder.Length > 0)
                lineBuilder.Append("\n"); // 恢复换行符
                
            lineBuilder.Append(currentLine);
            
            // 检查这一行中的引号数量，判断字段是否结束
            for (int i = 0; i < currentLine.Length; i++)
            {
                if (currentLine[i] == '"')
                {
                    // 处理双引号转义
                    if (i + 1 < currentLine.Length && currentLine[i + 1] == '"')
                    {
                        i++; // 跳过转义的双引号
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
            }
            
            // 如果不在引号内，说明当前行结束
            if (!inQuotes)
            {
                lineComplete = true;
            }
        }
        
        return lineBuilder.Length > 0 ? lineBuilder.ToString() : null;
    }

    // 解析CSV行，处理引号和逗号
    static string[] ParseCSVLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        string currentField = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentField);
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }


        result.Add(currentField);
        return result.ToArray();
    }
}