
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
            
            file1[0][3].Keyword.Should().Be("possible");
            file1[0][3][0].Keyword.Should().Be("age");
            file1[0][3][0].Value.Should().Be("define:NCharacters|ADULT_AGE");

            var file2 = folder1[1];

            file2.Keyword.Should().Be("FileExample2.txt");
            file2[1].Keyword.Should().Be("remove_command_from_ruler");
            file2[1][2].Keyword.Should().Be("potential");

            var comparison = new GameObject("potential", new []
            {
                new GameObject("is_ruler", "yes"),
                new GameObject("NOT", new []
                {
                    new GameObject("has_role", "general"),
                }),
                new GameObject("owner", "scope:actor"),
            }, 1);

            file1[0][2].ToString().Should().Be(comparison.ToString());
            Assert.IsTrue(comparison == file1[0][2]);

            var comparison2 = new GameObject("potential", new []
            {
                new GameObject("is_ruler", "no"),
                new GameObject("NOT", new []
                {
                    new GameObject("has_role", "general"),
                }),
                new GameObject("owner", "scope:actor"),
            }, 1);

            Assert.IsFalse(comparison2 == file1[0][2]);

            var comparison3 = new GameObject("potential", new []
            {
                new GameObject("is_ruler", "yes"),
                new GameObject("NOT", new []
                {
                    new GameObject("has_role", "Liutenant"),
                }),
                new GameObject("owner", "scope:actor"),
            }, 1);

            Assert.IsFalse(comparison3 == file1[0][2]);

            var comparison4 = new GameObject("potential", new []
            {
                new GameObject("is_ruler", "yes"),
                new GameObject("owner", "scope:actor"),
                new GameObject("NOT", new []
                {
                    new GameObject("has_role", "general"),
                }),
            }, 1);

            Assert.IsFalse(comparison4 == file1[0][2]);

            var comparison5 = new GameObject("potential", new []
            {
                new GameObject("is_ruler", "yes"),
                new GameObject("NOT", new []
                {
                    new GameObject("is_role", "general"),
                }),
                new GameObject("owner", "scope:actor"),
            }, 1);

            file1[0][2].ToString().Should().Be(comparison.ToString());
            Assert.IsFalse(comparison5 == file1[0][2]);
        }
    }
}