using Microsoft.CodeAnalysis;
using NMF.Models.Meta;

namespace CTMGenerator {

    public class LiteralConversionHelper : SymbolConversionHelper {

        public List<Literal> Literals { get; private set; }



        public LiteralConversionHelper() {
            Literals = [];
        }

        public void Reset() {
            Literals.Clear();
        }

        public void CleanConvert(List<IFieldSymbol> literalSymbols) {
            Reset();
            Convert(literalSymbols);
        }

        public void Convert(List<IFieldSymbol> literalSymbols) {
            foreach (IFieldSymbol literalSymbol in literalSymbols) {
                Literal literal = new() {
                    Name = literalSymbol.Name,
                    Value = (int?)literalSymbol.ConstantValue,
                    Remarks = ModelBuilderHelper.GetElementRemarks(literalSymbol),
                    Summary = ModelBuilderHelper.GetElementSummary(literalSymbol)
                };

                Literals.Add(literal);
            }
        }
    }
}
