
using Microsoft.VisualStudio.TestTools.UnitTesting;
using JoDiff.Models;
using System;
using FluentAssertions;
using System.Collections.Generic;

namespace JoDiff.UnitTests
{
    [TestClass]
    public class MiscTests
    {
        [TestMethod]
        public void RemoveComments()
        {
            var stringWithComments = @"
            questa linea non possiede commenti
            #però questa si
            a noi piacerebbe che questa stringa non possieda commenti #neanche questi in fondo alla riga
            
            pur mantendendo i returnCariage
            è molto importante controllare che la stringa abbia i cariageReturn normalizzati";
            var result = stringWithComments.RemoveComment();
            result.Should().Be(@"
            questa linea non possiede commenti
            
            a noi piacerebbe che questa stringa non possieda commenti 
            
            pur mantendendo i returnCariage
            è molto importante controllare che la stringa abbia i cariageReturn normalizzati");
        }

        [TestMethod]
        public void RemoveMultipleSpacesAndReturns()
        {
            var stringWithComments = @"Vogliamo che 
                    gli spazzi vengano eliminati e che 
                    
                    ogni 
                    
                    
                    riga
                    sia
                    
                    
                    collassata";
            var result = stringWithComments.RemoveMultipleWhiteSpacesAndReturns();
            result.Should().Be(@"Vogliamo che gli spazzi vengano eliminati e che ogni riga sia collassata");
        }

        [TestMethod]
        public void GetValuesBetweenBraketsPairs()
        {
            var stringWithComments = @"We need to get
                    {
                        the values between brackets pairs { it's important that respect the pairs in any condition }
                    }
                    so
                    
                    
                    we can { isolate {{{{ the valid variables}}}}
                    sia}
                    
                    
                    collassata".ReplaceLineEndings();
            var results = new List<string>();
            var index = 0;
            while(index > -1)
            {
                if(index+1 >= stringWithComments.Length) break;
                var result = stringWithComments[index..].GetNextValueBetweenBrackets(out index);
                index++; //skip the last closed bracket
                if(result != "") results.Add(result);
                else break;
            }
            results[0]
                .Should()
                .Be("the values between brackets pairs { it's important that respect the pairs in any condition }");
                
            results[1]
                .Should()
                .Be(@"isolate {{{{ the valid variables}}}}
                    sia".ReplaceLineEndings());
        }

        [TestMethod]
        public void GetValuesBetweenBraketsPairs2()
        {
            var stringWithComments = @"{ isolate {{{{ the valid variables}}}}
            }".ReplaceLineEndings();
            var results = new List<string>();
            var index = 0;
            while(index > -1)
            {
                if(index+1 >= stringWithComments.Length) break;
                var result = stringWithComments[index..].GetNextValueBetweenBrackets(out index);
                index++; //skip the last closed bracket
                if(result != "") results.Add(result);
                else break;
            }
            results[0]
                .Should()
                .Be(@"isolate {{{{ the valid variables}}}}".ReplaceLineEndings());
        }

        [TestMethod]
        public void GetbracketIndexes()
        {
            var stringWithComments = @"We need to get
                    {
                        the values between brackets pairs { it's important that respect the pairs in any condition }
                    }
                    and to exclude the rest";
            
            var result = stringWithComments.BraceMatch(out var firstInstance, out var lastInstance)[firstInstance..(lastInstance+1)];

            firstInstance
                .Should()
                .Be(36);

            lastInstance
                .Should()
                .Be(177);
                
            result.Should().Be(@"{
                        the values between brackets pairs { it's important that respect the pairs in any condition }
                    }");
        }
    }
}