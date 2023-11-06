
using Microsoft.VisualStudio.TestTools.UnitTesting;
using JoDiff.Models;
using FluentAssertions;

namespace JoDiff.UnitTests
{
    [TestClass]
    public class ParserTests
    {
        [TestMethod]
        public void TryParse()
        {
            var daGameData = GameData.GetGameData(GameEnum.Vic3, ".\\Files");
            daGameData.GameObject.Should().NotBeNull();
            foreach(var parameter in daGameData.GameObject)
            {
                parameter.Should().NotBeNull();
            }
            daGameData.GameObject.Should().NotBeEmpty();
        }
    }
}