
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using static Contensive.Processor.Tests.TestConstants;

namespace Contensive.Processor.Tests.UnitTests.Models.Db;
//
//====================================================================================================
/// <summary>
/// Job 20261126-contensive-render-speed, item 3. DbBaseModel.allowRecordCaching black-box matrix.
/// The probe is a stale read. A PersonModel is saved from the test method (a trusted caller, so it is cached), then the name is changed with raw sql
/// without invalidating the cache. A create that reads the cache returns the OLD name. A create that skips the cache returns the NEW name.
/// Callers with chosen namespaces are built with System.Reflection.Emit.
/// Run this matrix on unmodified dev first (it must pass), then on the changed code. It proves the change keeps today's results.
/// </summary>
[TestClass()]
public class AllowRecordCachingTests {
    //
    // ====================================================================================================
    // -- dynamic callers
    private static int assemblyCounter = 0;
    //
    private sealed class CallerDef {
        public string typeNamespace;
        public string typeName;
        public string kind; // id, guid, name, save
    }
    //
    /// <summary>
    /// Build a dynamic assembly. Each def becomes a type with a static method Run(...) that calls the matching DbBaseModel method for PersonModel.
    /// kind id:   PersonModel Run(CPBaseClass, int)           => DbBaseModel.create&lt;PersonModel&gt;(cp, id)
    /// kind guid: PersonModel Run(CPBaseClass, string)        => DbBaseModel.create&lt;PersonModel&gt;(cp, guid)
    /// kind name: PersonModel Run(CPBaseClass, string)        => DbBaseModel.createByUniqueName&lt;PersonModel&gt;(cp, name)
    /// kind save: int Run(CPBaseClass, PersonModel)           => person.save(cp)
    /// Returns the delegates keyed by "namespace.typeName.kind".
    /// </summary>
    private static Dictionary<string, Delegate> buildCallers(params CallerDef[] defs) {
        int n = Interlocked.Increment(ref assemblyCounter);
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"AllowRecordCachingDynamic{n}"), AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule($"AllowRecordCachingDynamic{n}");
        var typeBuilders = new List<(CallerDef def, TypeBuilder tb)>();
        foreach (CallerDef def in defs) {
            TypeBuilder tb = moduleBuilder.DefineType($"{def.typeNamespace}.{def.typeName}{def.kind}", TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Abstract);
            switch (def.kind) {
                case "id": {
                        MethodInfo create = typeof(DbBaseModel).GetMethods().Single(m => m.Name == "create" && m.IsGenericMethodDefinition && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType == typeof(int)).MakeGenericMethod(typeof(PersonModel));
                        emitStatic(tb, typeof(PersonModel), [typeof(CPBaseClass), typeof(int)], il => { il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Call, create); });
                        break;
                    }
                case "guid": {
                        MethodInfo create = typeof(DbBaseModel).GetMethods().Single(m => m.Name == "create" && m.IsGenericMethodDefinition && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType == typeof(string)).MakeGenericMethod(typeof(PersonModel));
                        emitStatic(tb, typeof(PersonModel), [typeof(CPBaseClass), typeof(string)], il => { il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Call, create); });
                        break;
                    }
                case "name": {
                        MethodInfo create = typeof(DbBaseModel).GetMethods().Single(m => m.Name == "createByUniqueName" && m.IsGenericMethodDefinition && m.GetParameters().Length == 2).MakeGenericMethod(typeof(PersonModel));
                        emitStatic(tb, typeof(PersonModel), [typeof(CPBaseClass), typeof(string)], il => { il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Call, create); });
                        break;
                    }
                case "save": {
                        MethodInfo save = typeof(DbBaseModel).GetMethod("save", [typeof(CPBaseClass)]);
                        emitStatic(tb, typeof(int), [typeof(CPBaseClass), typeof(PersonModel)], il => { il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Callvirt, save); });
                        break;
                    }
                default: throw new ArgumentException(def.kind);
            }
            typeBuilders.Add((def, tb));
        }
        var result = new Dictionary<string, Delegate>();
        foreach (var (def, tb) in typeBuilders) {
            Type created = tb.CreateType();
            MethodInfo run = created.GetMethod("Run");
            Type delegateType = def.kind switch {
                "id" => typeof(Func<CPBaseClass, int, PersonModel>),
                "guid" or "name" => typeof(Func<CPBaseClass, string, PersonModel>),
                _ => typeof(Func<CPBaseClass, PersonModel, int>)
            };
            result.Add($"{def.typeNamespace}.{def.typeName}.{def.kind}", run.CreateDelegate(delegateType));
        }
        return result;
    }
    //
    private static void emitStatic(TypeBuilder tb, Type returnType, Type[] parameterTypes, Action<ILGenerator> body) {
        MethodBuilder mb = tb.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, returnType, parameterTypes);
        ILGenerator il = mb.GetILGenerator();
        body(il);
        il.Emit(OpCodes.Ret);
    }
    //
    // ====================================================================================================
    // -- probe
    private static bool cacheEnabled(CPClass cp) => cp.ServerConfig.enableLocalFileCache || cp.ServerConfig.enableLocalMemoryCache || cp.ServerConfig.enableRemoteCache;
    //
    private sealed class Probe : IDisposable {
        private readonly CPClass cp;
        public PersonModel person;
        public string oldName;
        public string newName;
        public Probe(CPClass cp) {
            this.cp = cp;
            oldName = $"arcOld{cp.Utils.GetRandomInteger()}";
            newName = $"arcNew{cp.Utils.GetRandomInteger()}";
            person = DbBaseModel.addDefault<PersonModel>(cp);
            person.name = oldName;
            // -- save from the test method: trusted caller, record is cached
            person.save(cp);
            Thread.Sleep(30);
            // -- change the name behind the cache's back
            cp.Db.ExecuteNonQuery($"update ccmembers set name={cp.Db.EncodeSQLText(newName)} where id={person.id}");
        }
        /// <summary>
        /// true if the name returned came from the stale cache
        /// </summary>
        public bool isStale(PersonModel read) {
            Assert.IsNotNull(read);
            if (read.name == oldName) { return true; }
            Assert.AreEqual(newName, read.name);
            return false;
        }
        public void Dispose() {
            if (person != null && person.id > 0) { DbBaseModel.delete<PersonModel>(cp, person.id); }
        }
    }
    //
    // ====================================================================================================
    // -- a. caller is this test (namespace Contensive.Processor.Tests...), cached
    [TestMethod]
    public void allowRecordCaching_a_testMethod_isCached() {
        using (CPClass cp = new(testAppName)) {
            if (!cacheEnabled(cp)) { Assert.Inconclusive("cache not enabled"); }
            using var probe = new Probe(cp);
            Assert.IsTrue(probe.isStale(DbBaseModel.create<PersonModel>(cp, probe.person.id)));
            // h. second call, memo path
            Assert.IsTrue(probe.isStale(DbBaseModel.create<PersonModel>(cp, probe.person.id)));
            // i. guid variant
            Assert.IsTrue(probe.isStale(DbBaseModel.create<PersonModel>(cp, probe.person.ccguid)));
        }
    }
    //
    // -- b. caller is an addon assembly, Acme.Addon, not cached (new name)
    [TestMethod]
    public void allowRecordCaching_b_addonAssembly_notCached() {
        using (CPClass cp = new(testAppName)) {
            if (!cacheEnabled(cp)) { Assert.Inconclusive("cache not enabled"); }
            var callers = buildCallers(
                new CallerDef { typeNamespace = "Acme.Addon", typeName = "Caller", kind = "id" },
                new CallerDef { typeNamespace = "Acme.Addon", typeName = "GuidCaller", kind = "guid" });
            var byId = (Func<CPBaseClass, int, PersonModel>)callers["Acme.Addon.Caller.id"];
            var byGuid = (Func<CPBaseClass, string, PersonModel>)callers["Acme.Addon.GuidCaller.guid"];
            using var probe = new Probe(cp);
            Assert.IsFalse(probe.isStale(byId(cp, probe.person.id)));
            Assert.IsFalse(probe.isStale(byId(cp, probe.person.id)), "memo path");
            Assert.IsFalse(probe.isStale(byGuid(cp, probe.person.ccguid)));
        }
    }
    //
    // -- c. a dynamic assembly where every type is in Contensive.Processor.*, cached
    [TestMethod]
    public void allowRecordCaching_c_processorNamespaceAssembly_isCached() {
        using (CPClass cp = new(testAppName)) {
            if (!cacheEnabled(cp)) { Assert.Inconclusive("cache not enabled"); }
            var callers = buildCallers(new CallerDef { typeNamespace = "Contensive.Processor.Fake", typeName = "Caller", kind = "id" });
            var byId = (Func<CPBaseClass, int, PersonModel>)callers["Contensive.Processor.Fake.Caller.id"];
            using var probe = new Probe(cp);
            Assert.IsTrue(probe.isStale(byId(cp, probe.person.id)));
            Assert.IsTrue(probe.isStale(byId(cp, probe.person.id)), "memo path");
        }
    }
    //
    // -- d. mixed assembly: result depends on the immediate caller (the stack walk is used)
    [TestMethod]
    public void allowRecordCaching_d_mixedAssembly_followsImmediateCaller() {
        using (CPClass cp = new(testAppName)) {
            if (!cacheEnabled(cp)) { Assert.Inconclusive("cache not enabled"); }
            var callers = buildCallers(
                new CallerDef { typeNamespace = "Acme", typeName = "X", kind = "id" },
                new CallerDef { typeNamespace = "Contensive.Processor", typeName = "Y", kind = "id" });
            var fromAcme = (Func<CPBaseClass, int, PersonModel>)callers["Acme.X.id"];
            var fromProcessor = (Func<CPBaseClass, int, PersonModel>)callers["Contensive.Processor.Y.id"];
            using var probe = new Probe(cp);
            Assert.IsFalse(probe.isStale(fromAcme(cp, probe.person.id)), "d1");
            Assert.IsTrue(probe.isStale(fromProcessor(cp, probe.person.id)), "d2");
            Assert.IsFalse(probe.isStale(fromAcme(cp, probe.person.id)), "d1 again");
            Assert.IsTrue(probe.isStale(fromProcessor(cp, probe.person.id)), "d2 again");
        }
    }
    //
    // -- e. a helper type in Contensive.Models.Db (a models frame) called from this test: the walk continues to this test, cached
    [TestMethod]
    public void allowRecordCaching_e_modelsNamespaceHelper_walksToTestMethod() {
        using (CPClass cp = new(testAppName)) {
            if (!cacheEnabled(cp)) { Assert.Inconclusive("cache not enabled"); }
            var callers = buildCallers(new CallerDef { typeNamespace = "Contensive.Models.Db", typeName = "FakeHelper", kind = "id" });
            var helper = (Func<CPBaseClass, int, PersonModel>)callers["Contensive.Models.Db.FakeHelper.id"];
            using var probe = new Probe(cp);
            Assert.IsTrue(probe.isStale(helper(cp, probe.person.id)));
            Assert.IsTrue(probe.isStale(helper(cp, probe.person.id)), "memo path");
        }
    }
    //
    // -- f. a derived type outside the models namespace is never cached
    private class TestPerson : PersonModel { }
    //
    [TestMethod]
    public void allowRecordCaching_f_derivedType_notCached() {
        using (CPClass cp = new(testAppName)) {
            if (!cacheEnabled(cp)) { Assert.Inconclusive("cache not enabled"); }
            using var probe = new Probe(cp);
            var read = DbBaseModel.create<TestPerson>(cp, probe.person.id);
            Assert.IsFalse(probe.isStale(read));
            read = DbBaseModel.create<TestPerson>(cp, probe.person.id);
            Assert.IsFalse(probe.isStale(read), "memo path");
        }
    }
    //
    // -- g. called through reflection: the first frame is outside Contensive, not cached
    [TestMethod]
    public void allowRecordCaching_g_reflectionInvoke_notCached() {
        using (CPClass cp = new(testAppName)) {
            if (!cacheEnabled(cp)) { Assert.Inconclusive("cache not enabled"); }
            MethodInfo create = typeof(DbBaseModel).GetMethods().Single(m => m.Name == "create" && m.IsGenericMethodDefinition && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType == typeof(int)).MakeGenericMethod(typeof(PersonModel));
            using var probe = new Probe(cp);
            Assert.IsFalse(probe.isStale((PersonModel)create.Invoke(null, [cp, probe.person.id])));
            Assert.IsFalse(probe.isStale((PersonModel)create.Invoke(null, [cp, probe.person.id])), "memo path");
        }
    }
    //
    // -- i. createByUniqueName variants of a and b. People names are not unique (derivedNameFieldIsUnique false), so the name path is never cached. Both callers read sql.
    [TestMethod]
    public void allowRecordCaching_i_createByUniqueName_variants() {
        using (CPClass cp = new(testAppName)) {
            if (!cacheEnabled(cp)) { Assert.Inconclusive("cache not enabled"); }
            var callers = buildCallers(new CallerDef { typeNamespace = "Acme.Addon", typeName = "NameCaller", kind = "name" });
            var byName = (Func<CPBaseClass, string, PersonModel>)callers["Acme.Addon.NameCaller.name"];
            using var probe = new Probe(cp);
            // -- the record is found by its new name through sql, from the addon caller and from this test
            Assert.AreEqual(probe.newName, byName(cp, probe.newName).name);
            Assert.AreEqual(probe.newName, DbBaseModel.createByUniqueName<PersonModel>(cp, probe.newName).name);
        }
    }
    //
    // -- j. save from a denied caller invalidates only the record key. save from a trusted caller also advances the table dependency key
    [TestMethod]
    public void allowRecordCaching_j_save_tableKeyOnlyAdvancedByTrustedCaller() {
        using (CPClass cp = new(testAppName)) {
            if (!cacheEnabled(cp)) { Assert.Inconclusive("cache not enabled"); }
            var callers = buildCallers(new CallerDef { typeNamespace = "Acme.Addon", typeName = "Saver", kind = "save" });
            var denySave = (Func<CPBaseClass, PersonModel, int>)callers["Acme.Addon.Saver.save"];
            using var probe = new Probe(cp);
            using var other = new Probe(cp);
            //
            // -- save other from a denied caller. The table key is not advanced, so probe's cached record is still served (stale)
            var otherModel = DbBaseModel.create<PersonModel>(cp, other.person.id);
            denySave(cp, otherModel);
            Thread.Sleep(30);
            Assert.IsTrue(probe.isStale(DbBaseModel.create<PersonModel>(cp, probe.person.id)), "deny save does not advance the table key");
            //
            // -- save other from this test (trusted). The table key is advanced, so probe's cached record is invalidated and re-read (new name)
            otherModel = DbBaseModel.create<PersonModel>(cp, other.person.id);
            otherModel.save(cp);
            Thread.Sleep(30);
            Assert.IsFalse(probe.isStale(DbBaseModel.create<PersonModel>(cp, probe.person.id)), "trusted save advances the table key");
        }
    }
    //
    // -- no database needed: the classification of calling assemblies (private classifyAssembly and getCallerTrust, called by reflection)
    //    The test callers are dynamic assemblies. getCallerTrust never classifies a dynamic assembly (review m4), so classifyAssembly is called directly
    //    to check the namespace rules, and getCallerTrust is checked to return Walk for them.
    [TestMethod]
    public void allowRecordCaching_callerTrust_classification_noDb() {
        MethodInfo getCallerTrust = typeof(DbBaseModel).GetMethod("getCallerTrust", BindingFlags.NonPublic | BindingFlags.Static);
        MethodInfo classifyAssembly = typeof(DbBaseModel).GetMethod("classifyAssembly", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(getCallerTrust);
        Assert.IsNotNull(classifyAssembly);
        string trust(Delegate d) => classifyAssembly.Invoke(null, [d.Method.DeclaringType.Assembly]).ToString();
        string callerTrust(Delegate d) => getCallerTrust.Invoke(null, [null, d.Method.DeclaringType.Assembly]).ToString();
        var deny = buildCallers(new CallerDef { typeNamespace = "Acme.Addon", typeName = "Caller", kind = "id" });
        var allow = buildCallers(new CallerDef { typeNamespace = "Contensive.Processor.Fake", typeName = "Caller", kind = "id" });
        var allowTests = buildCallers(new CallerDef { typeNamespace = "Tests.Fake", typeName = "Caller", kind = "id" });
        var mixed = buildCallers(new CallerDef { typeNamespace = "Acme", typeName = "X", kind = "id" }, new CallerDef { typeNamespace = "Contensive.Processor", typeName = "Y", kind = "id" });
        var models = buildCallers(new CallerDef { typeNamespace = "Contensive.Models.Db", typeName = "FakeHelper", kind = "id" });
        Assert.AreEqual("Deny", trust(deny.Values.First()));
        Assert.AreEqual("Allow", trust(allow.Values.First()));
        Assert.AreEqual("Allow", trust(allowTests.Values.First()));
        Assert.AreEqual("Walk", trust(mixed.Values.First()));
        Assert.AreEqual("Walk", trust(models.Values.First()));
        // -- the models assembly itself and a null assembly always walk
        Assert.AreEqual("Walk", getCallerTrust.Invoke(null, [null, typeof(DbBaseModel).Assembly]).ToString());
        Assert.AreEqual("Walk", getCallerTrust.Invoke(null, [null, null]).ToString());
        // -- this test assembly is Contensive.Processor.Tests.* only, plus any generated types
        Assert.AreEqual("Deny", getCallerTrust.Invoke(null, [null, typeof(string).Assembly]).ToString());
        // -- dynamic assemblies always walk, whatever their namespaces (review m4)
        Assert.AreEqual("Walk", callerTrust(deny.Values.First()));
        Assert.AreEqual("Walk", callerTrust(allow.Values.First()));
        // -- a non-dynamic assembly is classified and memoized: second answer is the same
        Assert.AreEqual("Deny", getCallerTrust.Invoke(null, [null, typeof(string).Assembly]).ToString());
    }
    //
    // ====================================================================================================
    // -- review m4: global-namespace types named "<...". Known compiler-generated families are ignored, any other one makes the assembly walk
    private static Assembly buildAssemblyWithTypes(params string[] fullTypeNames) {
        int n = Interlocked.Increment(ref assemblyCounter);
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"AllowRecordCachingTypes{n}"), AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule($"AllowRecordCachingTypes{n}");
        foreach (string name in fullTypeNames) {
            moduleBuilder.DefineType(name, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Abstract).CreateType();
        }
        return assemblyBuilder;
    }
    //
    [TestMethod]
    public void allowRecordCaching_classify_compilerGeneratedNames_noDb() {
        MethodInfo classifyAssembly = typeof(DbBaseModel).GetMethod("classifyAssembly", BindingFlags.NonPublic | BindingFlags.Static);
        string classify(Assembly a) => classifyAssembly.Invoke(null, [a]).ToString();
        // -- known compiler-generated families do not change the answer
        Assert.AreEqual("Deny", classify(buildAssemblyWithTypes("Acme.Addon.Caller", "<PrivateImplementationDetails>", "<>f__AnonymousType0`2", "<>z__ReadOnlyArray`1", "<>y__InlineArray2`1")));
        Assert.AreEqual("Allow", classify(buildAssemblyWithTypes("Contensive.Processor.Fake.Caller", "<PrivateImplementationDetails>", "<>f__AnonymousType0`2")));
        // -- any other global "<" type may hold user code (C# 9 top-level statements, global file-local type): walk
        Assert.AreEqual("Walk", classify(buildAssemblyWithTypes("Contensive.Processor.Fake.Caller", "<Program>$")));
        Assert.AreEqual("Walk", classify(buildAssemblyWithTypes("Acme.Addon.Caller", "<File1>F0123456789ABCDEF__Helper")));
        // -- a global type without "<" is judged like the old walk judged it (null namespace = not trusted)
        Assert.AreEqual("Deny", classify(buildAssemblyWithTypes("GlobalHelper")));
        Assert.AreEqual("Walk", classify(buildAssemblyWithTypes("GlobalHelper", "Contensive.Processor.Fake.Caller")));
    }
    //
    // ====================================================================================================
    // -- review m3: the host shortcut trusts only the real Processor CPClass from the Processor assembly, not any CPBaseClass implementation
    //
    /// <summary>
    /// Build a CPBaseClass subclass in a dynamic Acme.* assembly. Every abstract member throws. Returns an instance created without a constructor call.
    /// </summary>
    private static CPBaseClass buildFakeCp() {
        int n = Interlocked.Increment(ref assemblyCounter);
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"AcmeFakeCp{n}"), AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule($"AcmeFakeCp{n}");
        TypeBuilder tb = moduleBuilder.DefineType("Acme.Addon.FakeCp", TypeAttributes.Public | TypeAttributes.Class, typeof(CPBaseClass));
        ConstructorInfo notImplemented = typeof(NotImplementedException).GetConstructor(Type.EmptyTypes);
        for (Type t = typeof(CPBaseClass); t != null && t != typeof(object); t = t.BaseType) {
            foreach (MethodInfo m in t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Where(m => m.IsAbstract)) {
                MethodBuilder mb = tb.DefineMethod(m.Name, (m.Attributes & ~MethodAttributes.Abstract & ~MethodAttributes.NewSlot) | MethodAttributes.Virtual | MethodAttributes.HideBySig, m.ReturnType, m.GetParameters().Select(p => p.ParameterType).ToArray());
                ILGenerator il = mb.GetILGenerator();
                il.Emit(OpCodes.Newobj, notImplemented);
                il.Emit(OpCodes.Throw);
                tb.DefineMethodOverride(mb, m);
            }
        }
        Type fakeType = tb.CreateType();
        return (CPBaseClass)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(fakeType);
    }
    //
    [TestMethod]
    public void allowRecordCaching_hostShortcut_onlyProcessorCpClass_noDb() {
        MethodInfo isHostCall = typeof(DbBaseModel).GetMethod("isHostCall", BindingFlags.NonPublic | BindingFlags.Static);
        MethodInfo getCallerTrust = typeof(DbBaseModel).GetMethod("getCallerTrust", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(isHostCall);
        // -- the real host: Processor's CPClass, called from the Processor assembly (no constructor call, no database)
        CPBaseClass hostCp = (CPBaseClass)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(CPClass));
        Assembly processorAssembly = typeof(CPClass).Assembly;
        Assert.IsTrue((bool)isHostCall.Invoke(null, [hostCp, processorAssembly]));
        Assert.AreEqual("Allow", getCallerTrust.Invoke(null, [hostCp, processorAssembly]).ToString());
        // -- the real cp, but the call comes from another assembly
        Assert.IsFalse((bool)isHostCall.Invoke(null, [hostCp, typeof(AllowRecordCachingTests).Assembly]));
        Assert.IsFalse((bool)isHostCall.Invoke(null, [null, processorAssembly]));
        // -- an addon-side CPBaseClass implementation that passes its own cp from its own assembly: not the host, Deny or Walk (never the shortcut Allow)
        CPBaseClass fakeCp = buildFakeCp();
        Assembly fakeAssembly = fakeCp.GetType().Assembly;
        Assert.IsFalse((bool)isHostCall.Invoke(null, [fakeCp, fakeAssembly]));
        string fakeTrust = getCallerTrust.Invoke(null, [fakeCp, fakeAssembly]).ToString();
        Assert.IsTrue(fakeTrust == "Deny" || fakeTrust == "Walk", $"fake cp trust [{fakeTrust}]");
        MethodInfo classifyAssembly = typeof(DbBaseModel).GetMethod("classifyAssembly", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.AreEqual("Deny", classifyAssembly.Invoke(null, [fakeAssembly]).ToString(), "an Acme.* assembly is denied by the namespace rule");
    }
    //
    // -- benchmark, log only
    [TestMethod]
    public void allowRecordCaching_benchmark_cachedCreate() {
        using (CPClass cp = new(testAppName)) {
            if (!cacheEnabled(cp)) { Assert.Inconclusive("cache not enabled"); }
            using var probe = new Probe(cp);
            DbBaseModel.create<PersonModel>(cp, probe.person.id);
            const int count = 10000;
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < count; i++) { DbBaseModel.create<PersonModel>(cp, probe.person.id); }
            sw.Stop();
            Console.WriteLine($"allowRecordCaching benchmark: {count} cached create<PersonModel> calls in {sw.ElapsedMilliseconds}ms, {sw.Elapsed.TotalMilliseconds * 1000.0 / count:0.0}us per call");
        }
    }
}
