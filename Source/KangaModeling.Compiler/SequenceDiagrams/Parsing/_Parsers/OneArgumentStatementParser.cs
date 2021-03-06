﻿using System.Collections.Generic;

namespace KangaModeling.Compiler.SequenceDiagrams
{
    internal abstract class OneArgumentStatementParser : StatementParser
    {
        public override IEnumerable<Statement> Parse(Scanner scanner)
        {
            Token keyword = scanner.ReadWord();
            scanner.SkipWhiteSpaces();
            Token argument = scanner.ReadToEnd();
            yield return
                argument.IsEmpty()
                    ? CreateEmptyStatement(keyword, argument)
                    : CreateStatement(keyword, argument);
        }

        protected virtual Statement CreateEmptyStatement(Token keyword, Token emptyArgument)
        {
            return new MissingArgumentStatement(keyword, emptyArgument);
        }

        protected abstract Statement CreateStatement(Token keyword, Token argument);
    }
}