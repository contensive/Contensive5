
using Contensive.BaseClasses;

namespace TestAddon {
    public class AddonClass : AddonBaseClass {
        public override object Execute(CPBaseClass cp) {
            return "TestAddon - " + TestReference.ReferencedClass.referencedMethod();
        }
    }
}
