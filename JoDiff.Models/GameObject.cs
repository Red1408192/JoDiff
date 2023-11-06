using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace JoDiff.Models
{
    public class GameObject : List<KeyValuePair<string, GameObject>>
    {
        public ModelType Type { get; private set; }
        public int CurrentLevel { get; private set; }
        public string Keyword { get; private set; }
        public string Value { get; private set; }
        public bool HasValue { get; private set; }

        public static GameObject Parse(string objectName, string path, string text, bool isInFile, int currentLevel, ref int currentIndex)
        {
            var gameObject = new GameObject()
            {
                Keyword = objectName,
                CurrentLevel = currentLevel
            };


            if(isInFile)
            {
                if(text is null) throw new Exception("File wasn't read");

                var valueMatch = Regex.Match(text, "(?>.?\\=.?)(?<Value>[-%+\\w:._\\|\\/\\\"]+)");
                var objectMatch = Regex.Match(text, @"(?>.?\=.?)(?<Value>.?\{)");
                if(objectMatch.Success && (!valueMatch.Success || valueMatch.Index > objectMatch.Index))
                {
                    var tempIndex = 0;
                    var innerObjectMatch = text.GetNextValueBetweenBrackets(out tempIndex);
                    var innerIndex = 0;
                    do
                    {
                        var match = Regex.Match(innerObjectMatch[innerIndex..], @"(?<Keyword>[\w:._]+)(?=.?\=)");
                        if(!match.Success) break;
                        if(innerIndex == 0) innerIndex = match.Index;
                        gameObject.Add(new KeyValuePair<string, GameObject>(match.Groups[1].Value, GameObject.Parse(match.Groups[1].Value, path, innerObjectMatch[innerIndex..], true, ++currentLevel, ref innerIndex)));
                    }
                    while(innerIndex < innerObjectMatch.Length);
                    currentIndex += tempIndex+1;
                    return gameObject;
                }
                else if(valueMatch.Success)
                {
                    gameObject.Value = valueMatch.Groups[1].Value;
                    gameObject.HasValue = true;
                    gameObject.Type = gameObject.Value switch
                    {
                        var x when int.TryParse(x, out _) => ModelType.TypeInt,
                        var x when float.TryParse(x, out _) => ModelType.TypeDec, 
                        var x when Regex.Match(x, "(?>\")(.*)(?>\")").Success => ModelType.TypeString, 
                        _ => ModelType.TypeKeyword
                    };
                    currentIndex += valueMatch.Index + valueMatch.Value.Length;
                    return gameObject;
                }
                else return gameObject;
            }
            else
            {
                
                if(Directory.Exists(path))
                {
                    var files = Directory.EnumerateFileSystemEntries(path);
                    gameObject.AddRange(files.Select(x => {
                        var objectName = Regex.Match(x, @"[^\\]+$").Value;
                        var currentIndex = 0;
                        return new KeyValuePair<string, GameObject>(objectName, GameObject.Parse(objectName, x, null, false, ++currentLevel, ref currentIndex));
                    }));
                    return gameObject;
                }
                else if(File.Exists(path) && Regex.Match(path, @"[^.]+$").Value == "txt")
                {
                    var ParsedStream = File.ReadAllText(path)
                        .RemoveComment()
                        .RemoveMultipleWhiteSpacesAndReturns();
                    var fileIndex = 0;
                    do
                    {
                        var match = Regex.Match(ParsedStream[fileIndex..], @"(?<Keyword>[\w:._]+)(?=.?\=)");
                        if(!match.Success) break;
                        if(fileIndex == 0) fileIndex = match.Index;
                        gameObject.Add(new KeyValuePair<string, GameObject>(match.Groups[1].Value, GameObject.Parse(match.Groups[1].Value, path, ParsedStream[fileIndex..], true, ++currentLevel, ref fileIndex)));
                    }
                    while(fileIndex < ParsedStream.Length);

                    return gameObject;
                }
                else return gameObject;
            }
        }

        public override string ToString()
        {
            var value = HasValue? Value : "{" + Environment.NewLine + string.Join(Environment.NewLine, this.Select(x => x.Value)) + Environment.NewLine + "}";
            return $"{Keyword} = {value}";
        }

        public enum ModelType
        {
            TypeObject,
            TypeString,
            TypeInt,
            TypeDec,
            TypeConditional,
            TypeKeyword,
        }
    }
}


