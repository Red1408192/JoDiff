using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;

namespace JoDiff.Models
{
    public class JominiObject : List<JominiObject>, IEquatable<JominiObject>, IEquatable<string>, IEquatable<int>, IEquatable<float>
    {
        private static string[] conditionals = {"if", "not", "nor", "else", "else_if"};

        private JominiObject(){ }
        private JominiObject(string keyword)
        {
            Keyword = keyword;
        }

        public JominiObject(string keyword, object value, int currentLevel = 0)
        {
            Keyword = keyword;
            CurrentLevel = currentLevel;
            SetValue(value);
        }

        public JominiObject(string keyword, IEnumerable<JominiObject> objects, int currentLevel = 0)
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
        public int? Index { get; private set; }

        public JominiObject this[string key]
        {
            get => this.FirstOrDefault(x => x.Keyword == key);
        }

        public JominiObject this[string key, int index]
        {
            get => this.Where(x => x.Keyword == key && x.Index == index).FirstOrDefault();
        }

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

        private new void Add(JominiObject item)
        {
            HasValue = false;
            Value = null;
            base.Add(item);
        }

        public new void AddRange(IEnumerable<JominiObject> items)
        {
            HasValue = false;
            Value = null;
            base.AddRange(items);

            GetHashCode();
        }

        public static JominiObject Parse(string objectName, string path, string text, bool isInFile, int currentLevel, ref int currentIndex, bool isConditional = false, int? conditionalIndex = null)
        {
            if(currentLevel > 50) throw new Exception("Parsing recursion went too deep");

            var jominiObject = new JominiObject()
            {
                Keyword = objectName.Contains('.')? $"'{objectName}'" : objectName,
                CurrentLevel = currentLevel,
                Type = isConditional? ModelType.TypeConditional : ModelType.TypeObject,
                Index = conditionalIndex
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
                    var objIndex = 0;
                    do
                    {
                        var match = Regex.Match(innerObjectMatch[innerIndex..], @"(?<Keyword>[\w:._]+)(?=.?[\\=|\\<|\\>])");
                        if(!match.Success) break;
                        if(innerIndex == 0) innerIndex = match.Index;

                        var objName = match.Groups[1].Value;
                        var conditional = false;
                        if(conditionals.Contains(objName)) conditional = true;

                        jominiObject.Add(JominiObject.Parse(objName, path, innerObjectMatch[innerIndex..], true, currentLevel+1, ref innerIndex, conditional, objIndex++));
                    }
                    while(innerIndex < innerObjectMatch.Length);

                    currentIndex += tempIndex+1;
                    jominiObject.GetHashCode();
                    return jominiObject;
                }
                else if(valueMatch.Success)
                {
                    jominiObject.OperatorType = valueMatch.Value switch
                    {
                        var x when x.Contains("<=") => OperatorTypeEnum.MinorEqualThen,
                        var x when x.Contains(">=") => OperatorTypeEnum.MajorEqualThen,
                        var x when x.Contains("<") => OperatorTypeEnum.MinorThen,
                        var x when x.Contains(">") => OperatorTypeEnum.MajorThen,
                        var x when x.Contains("!=") => OperatorTypeEnum.Disequal,
                        _ => OperatorTypeEnum.Assign,
                    };

                    jominiObject.SetValue(valueMatch.Groups[1].Value);

                    currentIndex += valueMatch.Index + valueMatch.Value.Length;
                    jominiObject.Hash = jominiObject.GetHashCode();
                    return jominiObject;
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
                    jominiObject.AddRange(files.Select(x => {
                        var objectName = Regex.Match(x, @"[^\\|^\/]+$").Value;
                        var currentIndex = 0;
                        return JominiObject.Parse(objectName, x, null, false, 0, ref currentIndex);
                    }));

                    jominiObject.GetHashCode();
                    return jominiObject;
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
                            jominiObject.Add(JominiObject.Parse(match.Groups[1].Value, path, ParsedStream[fileIndex..], true, currentLevel, ref fileIndex));
                        }
                        catch (Exception e)
                        {
                            jominiObject.Add(new JominiObject("ERRROR: " + e.Message));
                        }
                    }
                    while(fileIndex < ParsedStream.Length);

                    jominiObject.GetHashCode();
                    return jominiObject;
                }
                else return jominiObject;
            }
        }

        public static bool operator ==(JominiObject obj1, Object obj2) => obj1.Equals(obj2);
        public static bool operator !=(JominiObject obj1, Object obj2) => !obj1.Equals(obj2);

        public JominiObjectDiff ParseDifference(JominiObject other, string source = "")
            => ParseDifference(this, other, source);
        public static JominiObjectDiff ParseDifference(JominiObject jominiObject, JominiObject other, string source = "")
        {
            if(jominiObject.GetHashCode() == other.GetHashCode())
            {
                return null; // no diff
            }
            if(jominiObject.Keyword != other.Keyword || jominiObject.HasValue != other.HasValue)
            {
                return new JominiObjectDiff(source + '.' + jominiObject.GetDetailedKeyword(), DifferenceType.Full, other.ToString(), jominiObject, other, jominiObject.FullCount() + other.FullCount());
            }
            if(jominiObject.HasValue && other.HasValue)
            {
                if(jominiObject.OperatorType != other.OperatorType)
                {
                    return new JominiObjectDiff(source + '.' + jominiObject.GetDetailedKeyword(), DifferenceType.OperatorDifference, other.GetOperator() + " " + other.Value, jominiObject, other, 2);
                }
                else
                {
                    return new JominiObjectDiff(source, DifferenceType.ValueDifference, other.Value, jominiObject, other, 1);
                }
            }
            var diffCandidate = new JominiObjectDiff(source, DifferenceType.ObjectDifference, other.ToString(), jominiObject, other, jominiObject.FullCount());
            
            var sourceObjCopy = jominiObject.Select(x => x).ToList();
            var targetObjCopy = other.Select(x => x).ToList();

            foreach(var param in jominiObject)
            {
                var index = other.IndexOf(param);
                if(index is not -1)
                {
                    var toIndex = jominiObject.IndexOf(param);
                    // if(toIndex != index) //LATER PLEASE
                    // {
                    //     //positional differences makes sense only at this level
                    //     diffCandidate.InnerDiffs.Add(new JominiObjectDiff(source + '.' + jominiObject.Keyword, DifferenceType.MovedParameter, index.ToString() + "->" + toIndex.ToString(), jominiObject, other));
                    // }
                    sourceObjCopy.Remove(param);
                    targetObjCopy.Remove(param);
                    diffCandidate.Differences -= 1;
                }
            }
            if(!sourceObjCopy.Any() && !targetObjCopy.Any()) return diffCandidate;

            JominiObjectDiff[][] compatibilityMatrix = new JominiObjectDiff[sourceObjCopy.Count][];
            for (int i = 0; i < sourceObjCopy.Count; i++)
            {
                compatibilityMatrix[i] = new JominiObjectDiff[targetObjCopy.Count];
                for (int j = 0; j < targetObjCopy.Count; j++)
                {
                    compatibilityMatrix[i][j] = sourceObjCopy[i].ParseDifference(targetObjCopy[j], source + '.' + sourceObjCopy[i].GetDetailedKeyword());
                }
            }

            for (int i = 0; i < sourceObjCopy.Count; i++)
            {
                var sourceObj = sourceObjCopy[i];
                var candidates = compatibilityMatrix[i]
                                    .OrderByDescending(x => x.Likeness)
                                    .GetEnumerator();
                
                while (candidates.MoveNext())
                {
                    var best = candidates.Current;
                    //if(best.DifferenceType is DifferenceType.Full) break; //|| (best.Differences > 2 && best.Likeness < 0.4f)

                    var otherTarget = targetObjCopy.IndexOf(best.To);
                    if(otherTarget == -1) continue;

                    var otherCandidates = compatibilityMatrix.Select(x => x[otherTarget]);

                    if(otherCandidates.MaxBy(x => x.Likeness).Likeness > best.Likeness) continue;

                    sourceObjCopy[i] = null;
                    targetObjCopy[targetObjCopy.IndexOf(best.To)] = null;
                    diffCandidate.InnerDiffs.Add(best);
                    break;
                }
            }
            sourceObjCopy = sourceObjCopy.Where(x => x is not null).ToList();
            targetObjCopy = targetObjCopy.Where(x => x is not null).ToList();

            if(!sourceObjCopy.Any() && !targetObjCopy.Any()) return diffCandidate;

            diffCandidate.InnerDiffs.AddRange(sourceObjCopy.Select(x => new JominiObjectDiff(source + '.' + jominiObject.Keyword, DifferenceType.RemovedParameter, $"[{jominiObject.IndexOf(x)}]", x, null, x.FullCount())));
            diffCandidate.InnerDiffs.AddRange(targetObjCopy.Select(x => new JominiObjectDiff(source + '.' + other.Keyword, DifferenceType.AddedParameter, x.ToString(), null, x, x.FullCount())));
            
            diffCandidate.Differences = diffCandidate.InnerDiffs.Sum(x => x.Differences);
            return diffCandidate;
        }

        public override bool Equals(Object obj) //this will stop immediatelly after finding the firt difference, the previus function will not
        {
            if(obj is JominiObject other)
            {
                return this.Equals(other);

                // if(HasValue || other.HasValue){
                //     return Keyword == other.Keyword && Value == other.Value;
                // }
                // else if(this.Count == other.Count){
                //     return this.Select((x, y) => new { Item = x, Index = y }).All(x => x.Item == other[x.Index]);
                // }
            }
            else return false;
        }

        public override int GetHashCode()
        {
            if(HasValue){
                if(Hash.HasValue) return Hash.Value;
                else
                {
                    Hash = HashCode.Combine(Keyword, OperatorType, Value);
                    return Hash.Value;
                }
            }
            else{
                if(Hash.HasValue) return Hash.Value;
                Hash = this.Aggregate(0, (x, y) => (x << 2) ^ HashCode.Combine(y.Keyword, y));
                return Hash.Value;
            }
        }


        private int? _fullCount = null;
        public int FullCount()
        {
            if(_fullCount is null) _fullCount = 1 + this.Sum(x => x.FullCount());
            return _fullCount.Value;
        }


        public string GetDetailedKeyword() => Type is ModelType.TypeConditional? Keyword + $"[{Index}]" : Keyword;
        public override string ToString()
        {
            //var keyword = Index != null? Keyword + $"[{Index}]" : Keyword;

            var value = HasValue? Value : "{" + Environment.NewLine + new string('\t', Math.Max(CurrentLevel, 0)) + string.Join(Environment.NewLine + new string('\t', CurrentLevel), this.Select(x => x)) + Environment.NewLine + new string('\t', Math.Max(CurrentLevel-1, 0)) +"}";
            return $"{Keyword} {GetOperator()} {value}";
        }

        public bool Equals(JominiObject other) => this.GetHashCode() == other.GetHashCode();

        public bool Equals(string other) => HasValue && other == Value;

        public bool Equals(int other) => HasValue && int.TryParse(Value, out var intValue) && other == intValue;

        public bool Equals(float other) => HasValue && float.TryParse(Value, out var floatValue) && other == floatValue;

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
            TypeParameter,
            TypeString,
            TypeInt,
            TypeDec,
            TypeConditional,
            TypeKeyword,
        }
        public enum DifferenceType
        {
            Full,
            ObjectDifference,
            ValueDifference,
            OperatorDifference,
            MovedParameter,
            AddedParameter,
            RemovedParameter
        }
        public class JominiObjectDiff
        {
            public JominiObjectDiff(string location, DifferenceType differenceType, string parameter, JominiObject from, JominiObject to, int diffs = 1)
            {
                From = from;
                To = to;
                Location = location;
                DifferenceType = differenceType;
                Parameter = parameter;
                Differences = diffs;
            }
        
            public JominiObject From { get; set; }
            public JominiObject To { get; set; }
            public string Location { get; set; }
            public List<JominiObjectDiff> InnerDiffs { get; set; } = new List<JominiObjectDiff>();
            public DifferenceType DifferenceType { get; set; }
            public string Parameter { get; set; }
            public int Differences {get; set;}
            // {
            //     get
            //     {
            //         return DifferenceType switch
            //         {
            //             DifferenceType.MovedParameter or
            //             DifferenceType.ValueDifference => 1,
            //             DifferenceType.OperatorDifference => 2,
            //             DifferenceType.RemovedParameter => From.FullCount(),
            //             DifferenceType.AddedParameter => To.FullCount(),
            //             DifferenceType.Full => From.FullCount() + To.FullCount(),
            //             DifferenceType.ObjectDifference => InnerDiffs.Select(x => x.Differences).Sum(),
            //             _ => 0
            //         };
            //     }
            // }

            public float Likeness => 1f - (float)Differences / (DifferenceType is DifferenceType.Full? From?.FullCount()??1 + To?.FullCount()??1 : (float)Math.Max(From?.FullCount()??1, To?.FullCount()??1));

            public IEnumerable<string> OutputJoDiffInstructions()
            {
                if(!InnerDiffs.Any()) return DifferenceType switch
                {
                    DifferenceType.Full => new [] { Regex.Replace(Location, $"{From.Keyword}^", $"[{From.Index}]") + " << " + Parameter },
                    DifferenceType.ValueDifference => new [] { Location + " = "  + Parameter },
                    DifferenceType.OperatorDifference => new [] { Location + " "  + Parameter },
                    DifferenceType.RemovedParameter => new [] { Location + " -= "  + Parameter },
                    DifferenceType.AddedParameter => new [] { Location + " += "  + Parameter },
                    DifferenceType.ObjectDifference => new [] { Regex.Replace(Location, $"{From.Keyword}^", "") + " = {"  + Parameter + " }" },
                    _ => new[] { "" }
                };

                return InnerDiffs.SelectMany(x => x.OutputJoDiffInstructions());
            }
        }
    }
}


