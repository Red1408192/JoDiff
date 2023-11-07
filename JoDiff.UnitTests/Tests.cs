
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

            var folder1 = daGameData.GameObject[0];
            folder1.Keyword.Should().Be("Category1");
            var file1 = folder1[0];

            file1.Keyword.Should().Be("FileExample1.txt");
            file1[0].Keyword.Should().Be("grant_command_to_ruler");
            file1[0][0].Keyword.Should().Be("icon");
            file1[0][0].Value.Should().Be("\"gfx/interface/character_panel/grant_command.dds\"");

            var file2 = folder1[1];

            file2.Keyword.Should().Be("FileExample2.txt");
            file2[1].Keyword.Should().Be("remove_command_from_ruler");
            file2[1][2].Keyword.Should().Be("potential");

            var comparison = new GameObject("potential")
            {
                new GameObject(){ },
                new GameObject(){ },
            };
        }
    }
}