using System;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace FileManager
{
    class Program
    {
        public class EmptyCommand : Exception { }
        public class InvalidCommand : Exception { }
        public class DirNotFile : Exception
        {
            public string Path;

            public DirNotFile(string path)
            {
                Path = path;
            }
        } //ошибка, если файл копируется в назначение существующего каталога
        public class FileNotDir : Exception
        {
            public string Path;

            public FileNotDir(string path)
            {
                Path = path;
            }
        } //ошибка, если каталог копируется в назначение существующего файла
        const string config_path = "config.json";
        const string init_path = "init.json";
        static bool error = false; //отметка ошибки последней команды
        static ArrayList tree = new ArrayList();
        static string tree_path = "";
        static string info = ""; //текст сообщения для поля с информацией
        static ConsoleColor DirColor = ConsoleColor.DarkYellow;
        static ConsoleColor DefaultColor = ConsoleColor.Gray; 
        static ConsoleColor ErrorColor = ConsoleColor.DarkRed;
        static string ui_splitline = ""; //символ для границы полей UI
        static int ui_splitline_len = 0; //длина границы полей UI
        static int ui_limit = 0; //предел отображения дерева
        static int ui_page = 0;
        //парсеры для командной строки
        const string pattern_com = @"\A\w+(?=\s)|\A\w+";
        const string pattern_path = @"(?<=\s)(?:\b.+)(?=\s\-)|(?<=\w{2}\s)(?:\b.+)";
        const string pattern_path_1 = @"(?<=\s).+(?=\,)";
        const string pattern_path_2 = @"(?<=\,\s).+(?=\s\-\w)|(?<=\,\s).+";
        const string pattern_path_ex = @"\'.+?\'";
        const string pattern_attr = @"-\w(?:\W\w+)|-\w";
        static void Main(string[] args)
        {
            Console.Title = "File Manager";
            GetCommand("gc"); //загрузка конфигурации
            GetCommand("gi"); //инициализация последних данных
            GetCommand("pu");
            do
            {
                ClearInfoUI();
                GetCommand(Console.ReadLine());
                GetCommand("si"); //сохранение последних данных
                GetCommand("pu");
            } while (true);
        }
        static void ClearInfoUI()
        {
            info = "";
            error = false;
        }
        static void ObjectDel(string path)
        {
            path = GetTruePath(path);
            if (IsDirectory(path))
            {
                DirDel(path);
            }
            else
            {
                File.Delete(path);
            }
        }
        static void ObjectCopy(string source_path, string dist_path, bool rewrite)
        {
            source_path = GetTruePath(source_path);
            dist_path = GetTruePath(dist_path);
            if (IsDirectory(source_path))
            {
                if (File.Exists(dist_path))
                {
                    throw new FileNotDir(dist_path);
                }
                DirCopy(source_path, dist_path, rewrite);
            }
            else
            {
                if (Directory.Exists(dist_path))
                {
                    throw new DirNotFile(dist_path);
                }
                File.Copy(source_path, dist_path, rewrite);            
            }
        }
        static void DirCopy(string source_path, string dist_path, bool rewrite)
        {
            if (!Directory.Exists(dist_path))
            {
                Directory.CreateDirectory(dist_path);
            }
            foreach (string dir_path in Directory.GetDirectories(source_path))
            {
                var dir_info = new DirectoryInfo(dir_path);
                var dir_name_new = $@"{dist_path}\{dir_info.Name}";
                DirCopy(dir_path, dir_name_new, rewrite);
            }
            foreach (string file_path in Directory.GetFiles(source_path))
            {
                File.Copy(file_path, $@"{dist_path}\{Path.GetFileName(file_path)}", rewrite);
            }
        }
        static void DirDel(string source_path)
        {
            foreach (string file_path in Directory.GetFiles(source_path))
            {
                File.Delete(file_path);
            }
            foreach (string dir_path in Directory.GetDirectories(source_path))
            {
                DirDel(dir_path);
            }
            Directory.Delete(source_path);
        }
        static string GetTruePath(string path)
        {
            path = Path.IsPathRooted(path) ? path : $@"{tree_path}\{path}";
            return Path.GetFullPath(path);
            
        }
        static void GetTree(string path)
        {
            path = GetTruePath(path);
            if(!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"'{path}'");
            }

            tree_path = path;
            tree.Clear();
            tree.AddRange(Directory.GetDirectories(tree_path));
            tree.AddRange(Directory.GetFiles(tree_path));
        }
        static void GetRoot()
        {
            if (Directory.GetDirectoryRoot(tree_path) != tree_path)
            {
                tree_path = Directory.GetParent(tree_path).FullName;
            }
        }
        static void PrintTree()
        {
            if (tree.Count == 0)
            {
                Console.WriteLine(" Пустой каталог");
                return;
            }
            for (int i = ui_limit * (ui_page - 1); i < Math.Min(ui_limit * ui_page, tree.Count); i++)
            {
                if (IsDirectory((string)tree[i]))
                {
                    Console.ForegroundColor = DirColor;
                    Console.WriteLine($" {new DirectoryInfo((string)tree[i]).Name}");
                    Console.ForegroundColor = DefaultColor;
                }
                else
                {
                    Console.WriteLine($" {Path.GetFileName((string)tree[i])}");
                }
            }
            if (ui_limit < tree.Count)
            {
                Console.WriteLine($"\n Страница {ui_page} из {GetValidMaxPage()}");
            }
        }
        static bool IsDirectory(string path)
        {
            if ((File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory)
            {
                return true;
            }
            return false;
        }
        static long GetDirLength(FileInfo[] files)
        {
            long sum = 0;
            foreach (FileInfo file in files)
            {
                sum += file.Length;
            }
            return sum;
        }
        static void GetObjectAttributes(string object_path)
        {
            object_path = GetTruePath(object_path);
            if (IsDirectory(object_path))
            {
                var dir_info = new DirectoryInfo(object_path);
                var dir_files = dir_info.GetFiles("", SearchOption.AllDirectories);
                info += $" Имя папки: {dir_info.Name}\n";
                info += $" Дата и время создания: {dir_info.CreationTime:G}\n";
                info += $" Дата и время изменения: {dir_info.LastWriteTime}\n";
                info += $" Содержит: файлов: {dir_files.Length}; папок: {dir_info.GetDirectories("", SearchOption.AllDirectories).Length}\n";
                info += $" Размер: {GetDirLength(dir_files)}";
            }
            else
            {
                var file_info = new FileInfo(object_path);
                info += $" Имя файла: {file_info.Name}\n";
                info += $" Дата и время создания: {file_info.CreationTime:G}\n";
                info += $" Дата и время изменения: {file_info.LastWriteTime}\n";
                info += $" Размер: {file_info.Length}";
            }
            var object_attr = GetSystemAttributes(object_path);
            if (object_attr.Count > 0)
            {
                info += $"\n Системные атрибуты:";
                foreach (string attr in object_attr)
                {
                    info += $" {attr};";
                }
            }
        }
        static ArrayList GetSystemAttributes(string path)
        {
            var attr_list = new ArrayList();
            var object_attr = File.GetAttributes(path);
            if ((object_attr & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                attr_list.Add("Только для чтения");
            }
            if ((object_attr & FileAttributes.Hidden) == FileAttributes.Hidden)
            {
                attr_list.Add("Скрытый");
            }
            if ((object_attr & FileAttributes.Compressed) == FileAttributes.Compressed)
            {
                attr_list.Add("Сжат");
            }
            if ((object_attr & FileAttributes.Encrypted) == FileAttributes.Encrypted)
            {
                attr_list.Add("Зашифрован");
            }
            if ((object_attr & FileAttributes.System) == FileAttributes.System)
            {
                attr_list.Add("Системный");
            }
            if ((object_attr & FileAttributes.Temporary) == FileAttributes.Temporary)
            {
                attr_list.Add("Временный");
            }
            return attr_list;
        }
        static void PrintUI()
        {
            Console.Clear();
            Console.WriteLine(ui_splitline);
            PrintTree();
            Console.WriteLine(ui_splitline);
            Console.ForegroundColor = error ? ErrorColor: DefaultColor;
            Console.WriteLine(info);
            Console.ForegroundColor = DefaultColor;
            Console.WriteLine(ui_splitline);
            Console.Write($" {tree_path}>");
        }
        static void ConfigUI(Config config)
        {
            ui_splitline = "";
            ui_splitline_len = config.SplitLineLength;
            for (int i = 0; i < ui_splitline_len; i++)
            {
                ui_splitline += config.SplitLineChar;
            }
            ui_limit = config.Limit;
        }
        static void SetConfigUI(int limit, int length)
        {
            var config = new Config(limit, length);
            var config_json = JsonSerializer.Serialize(config);
            File.WriteAllText(config_path, config_json);

            ConfigUI(config);
        }
        static void InitUi(Init init)
        {
            tree_path = init.TreePath;
            ui_page = init.Page;
            info = init.Info;
            error = init.Error;
            GetCommand($"ls {tree_path} -p {ui_page + 1}");
        }
        static void SaveLastToInit()
        {
            var init_object = new Init(tree_path, ui_page, info, error);
            var init_json = JsonSerializer.Serialize(init_object);
            File.WriteAllText(init_path, init_json);
        }
        static void GetObjectJson(string object_name)
        {
            Object json_object;
            string json_path;
            switch (object_name)
            {
                case "config":
                    json_path = config_path;
                    json_object = File.Exists(json_path) ? JsonSerializer.Deserialize<Config>(File.ReadAllText(json_path)) : (Config)ToDefaultJson(new Config(), json_path);
                    ConfigUI((Config)json_object);
                    break;
                case "init":
                    json_path = init_path;
                    json_object = File.Exists(json_path) ? JsonSerializer.Deserialize<Init>(File.ReadAllText(json_path)) : (Init)ToDefaultJson(new Init(), json_path);
                    InitUi((Init)json_object);
                    break;
                default:
                    throw new InvalidCommand();
            }           
        }
        static object ToDefaultJson(object json_object, string json_path)
        {
            var json_text = JsonSerializer.Serialize(json_object);
            File.WriteAllText(json_path, json_text);
            return json_object;
        }
        static void GetInfoHelp()
        {
            info = File.ReadAllText("help.txt");
        }
        static void GetCommand(string line)
        {
            try
            {
                GetCommandValue(line);
            }
            catch (EmptyCommand)
            {
                info = " Введена пустая команда.";
                SaveError();
            }
            catch (InvalidCommand)
            {
                info = " Неверно передана команда. Для списка команд введите h или help.";
                SaveError();
            }
            catch (FormatException)
            {
                info = " Неверно передана команда. Для списка команд введите h или help.";
                SaveError();
            }
            catch (FileNotFoundException ex)
            {
                info = $" Объект {ex.FileName} не существует.";
                SaveError();
            }
            catch (DirNotFile ex)
            {
                info = $" Существующий каталог {ex.Path} не является файлом.";
                SaveError();
            }
            catch (FileNotDir ex)
            {
                info = $" Существующий файл {ex.Path} не является каталогом.";
                SaveError();
            }
            catch (DirectoryNotFoundException ex)
            {
                info = $" Каталог {ParseExMessage(ex.Message)} не существует.";
                SaveError();
            }
            catch (UnauthorizedAccessException ex)
            {
                info = $" Доступ к объекту {ParseExMessage(ex.Message)} запрещен.";
                SaveError();
            }
            catch (IOException ex)
            {
                if (ex.HResult == -2147024864)
                {
                    info = $" Объект {ParseExMessage(ex.Message)} используется другим процессом.";
                }
                if (ex.HResult == -2147024816)
                {
                    info = $" Файл {ParseExMessage(ex.Message)} уже существует.";
                }
                SaveError();
            }
            catch (Exception ex)
            {
                info = ex.Message;
                SaveError();
            }
        }
        static void SaveError()
        {
            error = true;
            try
            {
                if (!Directory.Exists("errors"))
                {
                    Directory.CreateDirectory("errors");
                }
                var file_name = $"{DateTime.Now:yyyyMMddHHmmss}";
                var i = 0;
                while (File.Exists($@"errors\{file_name}.txt"))
                {
                    i++;
                    file_name = $"{file_name}{i}";
                }
                File.WriteAllText($@"errors\{file_name}.txt", info.Trim());
            }
            catch (Exception)
            {
                info += "\n Не удалось сохранить файл с текстом ошибки.";
            }
        }
        static void GetCommandValue(string line)
        {
            if (line.Length == 0)
            {
                throw new EmptyCommand();
            }
            switch (ParseCommand(line, pattern_com))
            {
                case "gc":
                    GetObjectJson("config");
                    break;
                case "gi":
                    GetObjectJson("init");
                    break;
                case "pu":
                    PrintUI();
                    break;
                case "si":
                    SaveLastToInit();
                    break;
                case "sc":
                    SetConfigUI(
                        IsExistAttr(line, "-l") ? GetValidLimit(Convert.ToInt32(ParseAttr(line, "-l"))) : ui_limit,
                        IsExistAttr(line, "-c") ? Convert.ToInt32(ParseAttr(line, "-c")) : ui_splitline_len);
                    ui_page = GetValidPage(1);
                    break;
                case "ls":                  
                    GetTree(ParseCommand(line, pattern_path));
                    ui_page = IsExistAttr(line, "-p") ? GetValidPage(Convert.ToInt32(ParseAttr(line, "-p"))) : GetValidPage(1);
                    break;
                case "lr":
                    GetRoot();
                    GetTree(tree_path);
                    ui_page = GetValidPage(1);
                    break;
                case "pg":
                    ui_page = GetValidPage(Convert.ToInt32(ParseCommand(line, pattern_path)));
                    break;
                case "pw":
                    ui_page = GetValidPage(ui_page - 1);
                    break;
                case "pe":
                    ui_page = GetValidPage(ui_page + 1);
                    break;
                case "ld":
                    GetObjectAttributes(ParseCommand(line, pattern_path));
                    break;
                case "cp":
                    ObjectCopy(ParseCommand(line, pattern_path_1), ParseCommand(line, pattern_path_2), IsExistAttr(line, "-r"));
                    GetTree(tree_path);
                    break;
                case "rm":
                    ObjectDel(ParseCommand(line, pattern_path));
                    GetTree(tree_path);
                    break;
                case "h":
                case "help":
                    GetInfoHelp();
                    break;
                case "ex":
                    Environment.Exit(0);
                    break;
                default:
                    throw new InvalidCommand();
            }
        }
        static int GetValidPage(int page)
        {
            page = Math.Max(page, 1);
            page = Math.Min(page, GetValidMaxPage());
            return page;
        }
        static int GetValidLimit(int limit)
        {
            return Math.Max(limit, 1);
        }
        static int GetValidMaxPage()
        {
            return (int)Math.Ceiling((double)tree.Count / ui_limit);
        }
        static string ParseExMessage(string message)
        {
            return Regex.Match(message, pattern_path_ex).Value.Trim('\'');
        }
        static string ParseCommand(string line, string pattern)
        {
            var match = Regex.Match(line, pattern);
            if (!match.Success)
            {
                throw new InvalidCommand();
            }
            return match.Value;
        }
        static string ParseAttr(string line, string attr)
        {
            foreach (Match match in Regex.Matches(line, pattern_attr))
            {
                var match_split = match.Value.Split(' ');
                if (match_split[0] == attr && match_split.Length > 1)
                {
                    return match_split[1];
                }    
            }
            return null;
        }
        static bool IsExistAttr(string line, string attr)
        {
            foreach (Match match in Regex.Matches(line, pattern_attr))
            {
                var match_split = match.Value.Split(' ');
                if (match_split[0] == attr)
                {
                    return true;
                }
            }
            return false;
        }
    }
}