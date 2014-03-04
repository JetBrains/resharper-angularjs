#region license
// Copyright 2014 JetBrains s.r.o.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.AngularJS.Psi.AngularJs.Parsing
{
    public partial class AngularJsLexerGenerated
    {
        private TokenNodeType currTokenType;

        private struct TokenPosition
        {
            public TokenNodeType CurrTokenType;
            public int YyBufferIndex;
            public int YyBufferStart;
            public int YyBufferEnd;
            public int YyLexicalState;
        }

        public virtual void Start()
        {
            Start(0, yy_buffer.Length, YYINITIAL);
        }

        public virtual void Start(int startOffset, int endOffset, uint state)
        {
            yy_buffer_index = startOffset;
            yy_buffer_start = startOffset;
            yy_buffer_end = startOffset;
            yy_eof_pos = endOffset;
            yy_lexical_state = (int)state;
            currTokenType = null;
        }

        public virtual void Advance()
        {
            LocateToken();
            currTokenType = null;
        }

        public virtual object CurrentPosition
        {
            get
            {
                TokenPosition tokenPosition;
                tokenPosition.CurrTokenType = currTokenType;
                tokenPosition.YyBufferIndex = yy_buffer_index;
                tokenPosition.YyBufferStart = yy_buffer_start;
                tokenPosition.YyBufferEnd = yy_buffer_end;
                tokenPosition.YyLexicalState = yy_lexical_state;
                return tokenPosition;
            }
            set
            {
                var tokenPosition = (TokenPosition)value;
                currTokenType = tokenPosition.CurrTokenType;
                yy_buffer_index = tokenPosition.YyBufferIndex;
                yy_buffer_start = tokenPosition.YyBufferStart;
                yy_buffer_end = tokenPosition.YyBufferEnd;
                yy_lexical_state = tokenPosition.YyLexicalState;
            }
        }

        public virtual TokenNodeType TokenType
        {
            get
            {
                LocateToken();
                return currTokenType;
            }
        }

        public virtual int TokenStart
        {
            get
            {
                LocateToken();
                return yy_buffer_start;
            }
        }

        public virtual int TokenEnd
        {
            get
            {
                LocateToken();
                return yy_buffer_end;
            }
        }

        public IBuffer Buffer
        {
            get { return yy_buffer; }
        }

        public int EOFPos
        {
            get { return yy_eof_pos; }
        }

        public uint LexerStateEx
        {
            get { return (uint)yy_lexical_state; }
        }

        // Why 7? I have no idea. Lots of ReSharper lexers use 7
        public int LexemIndent
        {
            get { return 7; }
        }

        private void LocateToken()
        {
            if (currTokenType == null)
                currTokenType = _locateToken();
        }

        private TokenNodeType makeToken(TokenNodeType type)
        {
            //yy_lexical_state = state;
            return currTokenType = type;
        }
    }
}