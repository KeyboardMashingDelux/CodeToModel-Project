using CTMLib;
using Microsoft.CodeAnalysis;
using NMF.Models.Meta;
using System.Xml.Linq;

namespace CTMGenerator {

    public class LiteralConversionHelper : SymbolConversionHelper {

        public List<Literal> Literals { get; private set; }



        public LiteralConversionHelper() { 
            Reset();
        }

        public void Reset() {
            Literals = [];
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
