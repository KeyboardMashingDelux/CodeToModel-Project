using Microsoft.CodeAnalysis;
using NMF.Models.Meta;

namespace CTMGenerator {

    /// <summary>
    /// <see cref="SymbolConversionHelper"/> for <see cref="IFieldSymbol"/>s.
    /// </summary>
    public class LiteralConversionHelper : SymbolConversionHelper {

        /// <summary>
        /// <see cref="List{T}"/> of converted <see cref="ILiteral"/>s.
        /// </summary>
        public List<ILiteral> Literals { get; private set; }



        /// <summary>
        /// Creates an empty <see cref="LiteralConversionHelper"/>.
        /// </summary>
        public LiteralConversionHelper() {
            Literals = [];
        }

        /// <summary>
        /// Resets an <see cref="LiteralConversionHelper"/>.
        /// </summary>
        public void Reset() {
            Literals.Clear();
        }

        /// <summary>
        /// Resets an <see cref="LiteralConversionHelper"/> and then starts the conversion process.
        /// </summary>
        /// <param name="literalSymbols">The literals to convert.</param>
        public void CleanConvert(List<IFieldSymbol> literalSymbols) {
            Reset();
            Convert(literalSymbols);
        }

        /// <summary>
        /// Converts the given <see cref="List{T}"/> of <see cref="IFieldSymbol"/>s.
        /// </summary>
        /// <remarks>
        /// Properies which become <see cref="ILiteral"/>s will be stored in <see cref="Literals"/>.
        /// </remarks>
        /// <param name="literalSymbols">The literals to convert.</param>
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
