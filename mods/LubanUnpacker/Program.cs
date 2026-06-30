using LubanUnpacker.Models;
using LubanUnpacker.Utils;
using Luban;
using Newtonsoft.Json;
using System.Collections;
using System.Reflection;

namespace LubanUnpacker
{
    class Program
    {
        static readonly Dictionary<string, Type> FileClassRegistry = new(StringComparer.OrdinalIgnoreCase)
        {
            { "tbachievement", typeof(cfg.achievement) },
            { "tbactions", typeof(cfg.action) },
            { "tbaudioclip", typeof(cfg.audioClip) },
            { "tbbackgroundaudios", typeof(cfg.backgroundAudio) },
            { "tbcarchatcontent", typeof(cfg.carChatContent) },
            { "tbcards", typeof(cfg.card) },
            { "tbchaptercontent", typeof(cfg.chapterContent) },
            { "tbchapterend", typeof(cfg.chapterEnd) },
            { "tbchapterexplored", typeof(cfg.chapterExplored) },
            { "tbcharacterarchives", typeof(cfg.characterArchives) },
            { "tbcondition", typeof(cfg.condition) },
            { "tbcorrectdata", typeof(cfg.correctData) },
            { "tbcustomrewired", typeof(cfg.customRewired) },
            { "tbdatasettlement", typeof(cfg.dataSettlement) },
            { "tbdefaultdata", typeof(cfg.defaultData) },
            { "tbevents", typeof(cfg.customEvent) },
            { "tbfantasy", typeof(cfg.fantasy) },
            { "tbgamesetting", typeof(cfg.gameSetting) },
            { "tbguidebook", typeof(cfg.guideBook) },
            { "tbheart", typeof(cfg.heart) },
            { "tbhovertip", typeof(cfg.hoverTip) },
            { "tbinventory", typeof(cfg.inventory) },
            { "tblogic", typeof(cfg.logic) },
            { "tbminigames", typeof(cfg.miniGame) },
            { "tbmultiplechoiseaction", typeof(cfg.multipleChoiseAction) },
            { "tbnotebookaudio", typeof(cfg.noteBookAudio) },
            { "tboptionscontent", typeof(cfg.optionContent) },
            { "tbpanoramic", typeof(cfg.panoramic) },
            { "tbphone", typeof(cfg.phone) },
            { "tbphotogame", typeof(cfg.photogame) },
            { "tbphotograph", typeof(cfg.photograph) },
            { "tbquestionanswer", typeof(cfg.questionAnswer) },
            { "tbquestionbank", typeof(cfg.question) },
            { "tbranking", typeof(cfg.ranking) },
            { "tbrewiredlinkdata", typeof(cfg.customRewiredLinkData) },
            { "tbrole", typeof(cfg.role) },
            { "tbruleinfo", typeof(cfg.ruleInfo) },
            { "tbscene", typeof(cfg.scene) },
            { "tbshortcutkey", typeof(cfg.shortcutKey) },
            { "tbsoundeffect", typeof(cfg.soundEffect) },
            { "tbstorylineline", typeof(cfg.storylineLineData) },
            { "tbstorylinenode", typeof(cfg.storylineNodeData) },
            { "tbtips", typeof(cfg.tipContent) },
            { "tbuiform", typeof(cfg.uiForm) },
            { "tbuiimagelanguage", typeof(cfg.uiImageLanguage) },
            { "tbuitextlanguage", typeof(cfg.uiTextLanguage) },
            { "tbunlockvideo", typeof(cfg.unlockVideo) },
            { "tbvideoplaying", typeof(cfg.videoPlaying) },
            { "tbwechatcontent", typeof(cfg.weChatContent) },
            { "tbwechatlist", typeof(cfg.weChatList) },
            { "tbwordsgame", typeof(cfg.wordsGame) },
            { "tbxbspace", typeof(cfg.xbSpaceLine) },
            { "tbxt_daijia", typeof(cfg.xt_DaiJia) },
            { "tbxt_dangan", typeof(cfg.xt_DangAn) },
            { "tbxt_jishi", typeof(cfg.xt_JiShi) },
            { "tbxt_zhidao", typeof(cfg.xt_ZhiDao) }
        };

        static void Main(string[] args)
        {
            Directory.CreateDirectory("Input");
            Directory.CreateDirectory("Output");

            Console.WriteLine("----------- LUBAN UNPACKER -----------");
            Console.WriteLine("1. Generate Model class with Serialize method");
            Console.WriteLine("2. Unpack Bytes to JSON");
            Console.WriteLine("3. Repack JSON to Bytes");

            string? choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    Generate();
                    break;

                case "2":
                    Unpack();
                    break;

                case "3":
                    Repack();
                    break;

                default:
                    Console.WriteLine("Invalid choice.");
                    break;
            }
        }

        static void Unpack()
        {
            string[] files = Directory.GetFiles("Input", "*.bytes");
            if (files.Length == 0)
            {
                Console.WriteLine("No .bytes files found in Input folder!");
                return;
            }

            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file).ToLower();
                if (FileClassRegistry.TryGetValue(fileName, out Type? classType) && classType != null)
                {
                    UnpackGeneric(file, classType, fileName);
                }
                else
                {
                    Console.WriteLine($"[SKIPPED] No class registered for: {fileName}.bytes");
                }
            }
        }

        static void UnpackGeneric(string inputPath, Type classType, string fileName)
        {
            byte[] rawBytes = File.ReadAllBytes(inputPath);
            ByteBuf buf = new(rawBytes);

            Type listType = typeof(List<>).MakeGenericType(classType);
            IList extractedData = (IList)Activator.CreateInstance(listType)!;

            int rowCount = buf.ReadSize();
            for (int i = 0; i < rowCount; i++)
            {
                object row = Activator.CreateInstance(classType, buf)!;
                extractedData.Add(row);
            }

            string json = JsonConvert.SerializeObject(extractedData, Formatting.Indented);
            string outputPath = Path.Combine("Output", $"{fileName}.json");
            File.WriteAllText(outputPath, json);

            Console.WriteLine($"[SUCCESS] Unpacked {fileName}.bytes");
        }

        static void Repack()
        {
            string[] jsonFiles = Directory.GetFiles("Output", "*.json");
            if (jsonFiles.Length == 0)
            {
                Console.WriteLine("No .json files found in Output folder!");
                return;
            }

            foreach (string jsonPath in jsonFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(jsonPath);
                if (FileClassRegistry.TryGetValue(fileName, out Type? classType) && classType != null)
                {
                    string modelClassName = $"LubanUnpacker.Models.{classType.Name}";
                    Type? modelType = Type.GetType(modelClassName);
                    if (modelType != null)
                    {
                        MethodInfo? method = typeof(Program).GetMethod(nameof(RepackGeneric), BindingFlags.Static | BindingFlags.NonPublic);
                        if (method != null)
                        {
                            MethodInfo genericMethod = method.MakeGenericMethod(modelType);
                            genericMethod.Invoke(null, [jsonPath, modelType, fileName]);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Error: Could not find a model named {modelClassName}. Run with option 1 to generate it!");
                    }
                }
                else
                {
                    Console.WriteLine($"[SKIPPED] No class registered for: {fileName}.json");
                }
            }
        }

        static void RepackGeneric<T>(string jsonPath, Type classType, string fileName) where T : ILubanModel, new()
        {
            try
            {
                string json = File.ReadAllText(jsonPath);

                List<T>? editedData = JsonConvert.DeserializeObject<List<T>>(json);
                if (editedData == null)
                {
                    Console.WriteLine("Error: JSON data was empty or corrupted.");
                    return;
                }

                ByteBuf buf = new();
                buf.WriteSize(editedData.Count);

                MethodInfo? serializeMethod = classType.GetMethod("Serialize");
                if (serializeMethod == null)
                {
                    Console.WriteLine($"[ERROR] The class {classType.Name} is missing a Serialize(ByteBuf) method!");
                    return;
                }

                foreach (T row in editedData)
                {
                    row.Serialize(buf);
                }

                string outputPath = Path.Combine("Output", $"{fileName}.bytes");
                File.WriteAllBytes(outputPath, buf.CopyData());

                Console.WriteLine($"[SUCCESS] Repacked {fileName}.bytes");
            }
            catch (Exception e)
            {
                Console.WriteLine("-------------------------------");
                Console.Error.WriteLine($"An error occurred for file [{fileName}] : {e.Message}");
                Console.Error.WriteLine($"Stack Trace:\n{e.StackTrace}");
            }
        }

        static void Generate()
        {
            foreach (var entry in FileClassRegistry)
            {
                TypeExporter.ExportToFile(entry.Value);
            }
        }
    }
}
