using NMF.Models.Meta;
using NMF.Models.Repository;

namespace CodeToModel {
    internal class Program {
        static void Main(string[] args) {
            Console.WriteLine("Hello, NMF Code to Model!");

            var repository = new ModelRepository();
            var ns = new Namespace();
            ns.Types.Add(new Class { Name = "TEST_CLASS"});
            repository.Save(ns, "TMP.nmeta");
        }
    }
}
