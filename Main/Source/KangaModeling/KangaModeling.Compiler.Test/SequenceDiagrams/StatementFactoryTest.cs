﻿using System;
using KangaModeling.Compiler.SequenceDiagrams._Parsing;
using NUnit.Framework;

namespace KangaModeling.Compiler.Test.SequenceDiagrams
{
    [TestFixture]
    public class StatementFactoryTest
    {
        [TestCase(null, typeof(UnknownStatementParser))]
        [TestCase("", typeof(UnknownStatementParser))]
        [TestCase("abd", typeof(UnknownStatementParser))]
        [TestCase("title", typeof(TitleStatementParser))]
        public void GetStatementParserTest(string keyword, Type expectedType)
        {
            var target = new StatementParserFactory();
            var actual = target.GetStatementParser(keyword);
            Assert.AreEqual(expectedType, actual.GetType());
        }
    }
}
