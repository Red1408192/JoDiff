
using Microsoft.VisualStudio.TestTools.UnitTesting;
using JoDiff.Models;
using FluentAssertions;

namespace JoDiff.UnitTests
{
    [TestClass]
    public class ParserTests
    {
        [TestMethod]
        public void TryParseDifference()
        {
            var daGameData = JominiData.GetJominiData(GameEnum.Vic3, ".\\Files");
            var ob1 = daGameData.GameObject[0][1]["grant_leadership_to_agitator"];
            var ob2 = daGameData.GameObject[1][0]["grant_leadership_to_agitator"];
            
            var result = ob1.ParseDifference(ob2, ob2.Keyword);
        }
        [TestMethod] //will divide in different functions later...
        public void TryParse()
        {
            var daJoData = JominiData.GetJominiData(GameEnum.Vic3, ".\\Files");
            daJoData.GameObject.Should().NotBeNull();
            foreach(var parameter in daJoData.GameObject)
            {
                parameter.Should().NotBeNull();
            }
            daJoData.GameObject.Should().NotBeEmpty();

            var folder1 = daJoData.GameObject[0];
            folder1.Keyword.Should().Be("Category1");
            var file1 = folder1[0];

            file1.Keyword.Should().Be("FileExample1.txt");
            file1[0].Keyword.Should().Be("grant_command_to_ruler");
            file1[0][2].FullCount().Should().Be(5);
            file1[0].FullCount().Should().Be(43);
            file1[0][0].Keyword.Should().Be("icon");
            file1[0][0].Value.Should().Be("\"gfx/interface/character_panel/grant_command.dds\"");
            
            file1[0][3].Keyword.Should().Be("possible");
            file1[0][3][0].Keyword.Should().Be("age");
            file1[0][3][0].Value.Should().Be("define:NCharacters|ADULT_AGE");

            var file2 = folder1[1];

            file2.Keyword.Should().Be("FileExample2.txt");
            file2[1].Keyword.Should().Be("remove_command_from_ruler");
            file2[1][2].Keyword.Should().Be("potential");

            var comparison = new JominiObject("potential", new []
            {
                new JominiObject("is_ruler", "yes"),
                new JominiObject("NOT", new []
                {
                    new JominiObject("has_role", "general"),
                }),
                new JominiObject("owner", "scope:actor"),
            }, 1);

            file1[0][2].ToString().Should().Be(comparison.ToString());
            Assert.IsTrue(comparison == file1[0][2]);

            var comparison2 = new JominiObject("potential", new []
            {
                new JominiObject("is_ruler", "no"),
                new JominiObject("NOT", new []
                {
                    new JominiObject("has_role", "general"),
                }),
                new JominiObject("owner", "scope:actor"),
            }, 1);

            Assert.IsFalse(comparison2 == file1[0][2]);

            var comparison3 = new JominiObject("potential", new []
            {
                new JominiObject("is_ruler", "yes"),
                new JominiObject("NOT", new []
                {
                    new JominiObject("has_role", "Liutenant"),
                }),
                new JominiObject("owner", "scope:actor"),
            }, 1);

            Assert.IsFalse(comparison3 == file1[0][2]);

            var comparison4 = new JominiObject("potential", new []
            {
                new JominiObject("is_ruler", "yes"),
                new JominiObject("owner", "scope:actor"),
                new JominiObject("NOT", new []
                {
                    new JominiObject("has_role", "general"),
                }),
            }, 1);

            Assert.IsFalse(comparison4 == file1[0][2]);

            var comparison5 = new JominiObject("potential", new []
            {
                new JominiObject("is_ruler", "yes"),
                new JominiObject("NOT", new []
                {
                    new JominiObject("is_role", "general"),
                }),
                new JominiObject("owner", "scope:actor"),
            }, 1);

            Assert.IsFalse(comparison5 == file1[0][2]);
            
            var comparison6 = new JominiObject("potential", new []
            {
                new JominiObject("is_ruler", "yes"),
                new JominiObject("AND", new []
                {
                    new JominiObject("has_role", "general"),
                }),
                new JominiObject("owner", "scope:actor"),
            }, 1);

            file1[0][2].ToString().Should().Be(comparison.ToString());
            Assert.IsFalse(comparison6 == file1[0][2]);
        }
    }
}