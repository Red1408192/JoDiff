using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace JoDiff.Models
{
    public class GameObject : List<GameObject>, IEquatable<GameObject>, IEquatable<string>, IEquatable<int>, IEquatable<float>
    {
        private GameObject(){ }
        private GameObject(string keyword)
        {
            Keyword = keyword;
        }

        public GameObject(string keyword, object value, int currentLevel = 0)
        {
            Keyword = keyword;
            CurrentLevel = currentLevel;
            SetValue(value);
        }

        public GameObject(string keyword, IEnumerable<GameObject> objects, int currentLevel = 0)
        {
            Keyword = keyword;
            CurrentLevel = currentLevel;
            foreach(var i in objects){
                i.CurrentLevel = CurrentLevel+1;
                this.Add(i);
            }
            GetHashCode();
        }

        public OperatorTypeEnum OperatorType { get; set; } = OperatorTypeEnum.Assign;
        public ModelType Type { get; set; }
        public int CurrentLevel { get; set; }
        public Guid Id { get; private set; } = Guid.NewGuid();
        public int? Hash { get; private set; }
        public string Keyword { get; set; }
        public string Value { get; private set; } //do not ovveride this
        public bool HasValue { get; set; }

        public void SetValue(object value) => SetValue(value.ToString());
        public void SetValue(string value)
        {
            if(this.Any()) this.RemoveRange(0, Count);
            HasValue = true;
            Type = value switch
            {
                var x when int.TryParse(x, out _) => ModelType.TypeInt,
                var x when float.TryParse(x, out _) => ModelType.TypeDec, 
                var x when Regex.Match(x, "(?>\")(.*)(?>\")").Success => ModelType.TypeString, 
                _ => ModelType.TypeKeyword
            };
            Value = value;
            GetHashCode();
        }

        private new void Add(GameObject item)
        {
            HasValue = false;
            Value = null;
            base.Add(item);
        }

        public new void AddRange(IEnumerable<GameObject> items)
        {
            HasValue = false;
            Value = null;
            base.AddRange(items);

            GetHashCode();
        }

        public static GameObject Parse(string objectName, string path, string text, bool isInFile, int currentLevel, ref int currentIndex)
        {
            if(currentLevel > 50) throw new Exception("Parsing recursion went too deep");

            var gameObject = new GameObject()
            {
                Keyword = objectName,
                CurrentLevel = currentLevel
            };


            if(isInFile)
            {
                if(text is null) throw new Exception("File wasn't read");

                var valueMatch = Regex.Match(text, "(?>.?[\\=|\\<|\\>].?)(?<Value>[-%+\\w:._\\|\\/\\\"]+)");
                var objectMatch = Regex.Match(text, @"(?>.?\=.?)(?<Value>.?\{)");
                if(objectMatch.Success && (!valueMatch.Success || valueMatch.Index > objectMatch.Index))
                {
                    var tempIndex = 0;
                    var innerObjectMatch = text.GetNextValueBetweenBrackets(out tempIndex);
                    var innerIndex = 0;
                    do
                    {
                        var match = Regex.Match(innerObjectMatch[innerIndex..], @"(?<Keyword>[\w:._]+)(?=.?[\\=|\\<|\\>])");
                        if(!match.Success) break;
                        if(innerIndex == 0) innerIndex = match.Index;
                        gameObject.Add(GameObject.Parse(match.Groups[1].Value, path, innerObjectMatch[innerIndex..], true, currentLevel+1, ref innerIndex));
                    }
                    while(innerIndex < innerObjectMatch.Length);

                    currentIndex += tempIndex+1;
                    gameObject.GetHashCode();
                    return gameObject;
                }
                else if(valueMatch.Success)
                {
                    gameObject.OperatorType = valueMatch.Value switch
                    {
                        var x when x.Contains("<=") => OperatorTypeEnum.MinorEqualThen,
                        var x when x.Contains(">=") => OperatorTypeEnum.MajorEqualThen,
                        var x when x.Contains("<") => OperatorTypeEnum.MinorThen,
                        var x when x.Contains(">") => OperatorTypeEnum.MajorThen,
                        var x when x.Contains("!=") => OperatorTypeEnum.Disequal,
                        _ => OperatorTypeEnum.Assign,
                    };

                    gameObject.SetValue(valueMatch.Groups[1].Value);

                    currentIndex += valueMatch.Index + valueMatch.Value.Length;
                    gameObject.Hash = gameObject.GetHashCode();
                    return gameObject;
                }
                else
                {
                    throw new Exception("Invalid File at line: " + currentIndex + "Level:" + currentLevel);
                }
            }
            else
            {
                
                if(Directory.Exists(path))
                {
                    var files = Directory.EnumerateFileSystemEntries(path);
                    gameObject.AddRange(files.Select(x => {
                        var objectName = Regex.Match(x, @"[^\\]+$").Value;
                        var currentIndex = 0;
                        return GameObject.Parse(objectName, x, null, false, 0, ref currentIndex);
                    }));

                    gameObject.GetHashCode();
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
                        try
                        {
                            gameObject.Add(GameObject.Parse(match.Groups[1].Value, path, ParsedStream[fileIndex..], true, currentLevel, ref fileIndex));
                        }
                        catch (Exception e)
                        {
                            gameObject.Add(new GameObject("ERRROR: " + e.Message));
                        }
                    }
                    while(fileIndex < ParsedStream.Length);

                    gameObject.GetHashCode();
                    return gameObject;
                }
                else return gameObject;
            }
        }

        public static bool operator ==(GameObject obj1, Object obj2) => obj1.Equals(obj2);
        public static bool operator !=(GameObject obj1, Object obj2) => !obj1.Equals(obj2);

        // public bool ParseDifference(this GameObject gameObject, GameObject other, out GameObjectDiff diffCandidate, string source = "", List<GameObjectDiff> gameObjectDiffs = null)
        // {
        //     if(gameObjectDiffs is null) gameObjectDiffs = new();
        //     diffCandidate = null;

        //     if(gameObject.GetHashCode() == other.GetHashCode()) return false; // no diff
        //     if(gameObject.HasValue != other.HasValue)
        //     {
        //         diffCandidate = new GameObjectDiff(source, DifferenceType.Full, other.ToString());
        //         return true;
        //     }
        //     if(gameObject.HasValue && other.HasValue)
        //     {
        //         if(gameObject.Keyword != other.Keyword)
        //         {
        //             ///??????
        //             return true;
        //         }
        //         else// if(gameObject.Value != other.Value)
        //         {
        //             diffCandidate = new GameObjectDiff(source, DifferenceType.ValueDifference, other.Value);
        //             return true;
        //         }
        //     }
        //     var diffCounts = 0;
        //     foreach(var param in this)
        //     {
        //         if(other.Contains(param)) continue;
        //         else diffCounts++;
        //     }
        //     if(diffCounts > (this.Count * 0.8))
        //     {
        //         diffCandidate = new GameObjectDiff(source, DifferenceType.Full, other.ToString());
        //     }


        //     if(other.Count > this.Count) diffCounts += other.Count - this.Count;

        // }

        public override bool Equals(Object obj) //this will stop immediatelly after finding the firt difference, the previus function will not
        {
            if(obj is GameObject other)
            {
                return this.GetHashCode() == other.GetHashCode();

                // if(HasValue || other.HasValue){
                //     return Keyword == other.Keyword && Value == other.Value;
                // }
                // else if(this.Count == other.Count){
                //     return this.Select((x, y) => new { Item = x, Index = y }).All(x => x.Item == other[x.Index]);
                // }
            }
            else if(obj is string otherString){
                return HasValue && otherString == Value; //does not compare the keyword
            }
            else{
                return HasValue && obj.ToString() == Value;  //does not compare the keyword
            }
            //return false;
        }

        public override int GetHashCode()
        {
            if(HasValue){
                if(Hash.HasValue) return Hash.Value;
                else
                {
                    Hash = Keyword.GetHashCode() ^ Value.GetHashCode();
                    return Hash.Value;
                }
            }
            else{
                if(Hash.HasValue) return Hash.Value;
                Hash = this.Aggregate(0, (x, y) => (x << 2) ^ y.GetHashCode());
                return Hash.Value;
            }
        }

        public override string ToString()
        {
            var value = HasValue? Value : "{" + Environment.NewLine + new string('\t', Math.Max(CurrentLevel, 0)) + string.Join(Environment.NewLine + new string('\t', CurrentLevel), this.Select(x => x)) + Environment.NewLine + new string('\t', Math.Max(CurrentLevel-1, 0)) +"}";
            return $"{Keyword} {GetOperator()} {value}";
        }

        public bool Equals(GameObject other) => this.Equals(other);

        public bool Equals(string other) => this.Equals(other);

        public bool Equals(int other) => this.Equals(other);

        public bool Equals(float other) => this.Equals(other);

        public string GetOperator() => OperatorType switch
        {
            OperatorTypeEnum.Assign => "=",
            OperatorTypeEnum.Disequal => "!=",
            OperatorTypeEnum.MinorThen => "<",
            OperatorTypeEnum.MinorEqualThen => "<=",
            OperatorTypeEnum.MajorThen => ">",
            OperatorTypeEnum.MajorEqualThen => ">=",
            _ => "="
        };

        public enum OperatorTypeEnum
        {
            Assign,
            Disequal,
            MinorThen,
            MajorThen,
            MinorEqualThen,
            MajorEqualThen,
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
        public enum DifferenceType{
            Full,
            ValueDifference,
            MovedParameter,
            AddedParameter,
            RemovedParameter
        }
        public class GameObjectDiff
        {
            public GameObjectDiff(string location, DifferenceType differenceType, string parameter)
            {
                Location = location;
                DifferenceType = differenceType;
                Parameter = parameter;
            }

            public string Location { get; set; }
            public DifferenceType DifferenceType { get; set; }
            public string Parameter { get; set; }
        }
    }
}


