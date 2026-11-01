using DocumentIntake;

var tests = new (string Name, Action<string> Run)[]
{
    ("Preview has no destination side effects", root => {
        var source = Input(root, "report.PDF", "sample");
        var target = Path.Combine(root, "output");
        var result = Intake.Run(source, target, false);
        Check(result.Planned == 1 && result.Copied == 0, "incorrect count");
        Check(result.Files[0].Category == "documents", "extension comparison");
        Check(!Directory.Exists(target), "preview wrote files");
    }),
    ("Apply preserves originals and contents", root => {
        var source = Input(root, "report.txt", "hello résumé");
        var target = Path.Combine(root, "output");
        Check(Intake.Run(source,target,true).Copied == 1,"not copied");
        Check(File.ReadAllText(Path.Combine(target,"documents","report.txt")) == "hello résumé","content mismatch");
        Check(File.Exists(Path.Combine(source,"report.txt")),"source deleted");
        Check(!Directory.EnumerateFiles(target,"*.tmp",SearchOption.AllDirectories).Any(),"temporary file remains");
    }),
    ("Unknown extension goes to other", root => {
        var source = Input(root,"payload.xyz","test");
        Check(Intake.Plan(source,Path.Combine(root,"output"))[0].Category == "other","incorrect category");
    }),
    ("Nested paths and duplicate basenames are preserved", root => {
        var source = Input(root,"a/report.csv","a"); Input(root,"b/report.csv","b");
        var target=Path.Combine(root,"output"); Intake.Run(source,target,true);
        Check(File.ReadAllText(Path.Combine(target,"data","a","report.csv"))=="a","first missing");
        Check(File.ReadAllText(Path.Combine(target,"data","b","report.csv"))=="b","second missing");
    }),
    ("Existing destination is never overwritten", root => {
        var source=Input(root,"a.txt","new");var target=Path.Combine(root,"output");
        Directory.CreateDirectory(Path.Combine(target,"documents"));
        File.WriteAllText(Path.Combine(target,"documents","a.txt"),"old");
        Throws<IOException>(()=>Intake.Run(source,target,true));
        Check(File.ReadAllText(Path.Combine(target,"documents","a.txt"))=="old","overwritten");
    }),
    ("Preflight conflict prevents earlier copies", root => {
        var source=Input(root,"a.txt","a");Input(root,"z.txt","z");var target=Path.Combine(root,"output");
        Directory.CreateDirectory(Path.Combine(target,"documents"));File.WriteAllText(Path.Combine(target,"documents","z.txt"),"old");
        Throws<IOException>(()=>Intake.Run(source,target,true));
        Check(!File.Exists(Path.Combine(target,"documents","a.txt")),"partial preflight write");
    }),
    ("Same and nested directories rejected", root => {
        var source=Input(root,"a.txt","a");
        Throws<ArgumentException>(()=>Intake.Plan(source,source));
        Throws<ArgumentException>(()=>Intake.Plan(source,Path.Combine(source,"out")));
        Throws<ArgumentException>(()=>Intake.Plan(source,root));
    }),
    ("Missing input rejected", root => Throws<DirectoryNotFoundException>(()=>Intake.Plan(Path.Combine(root,"missing"),Path.Combine(root,"output")))),
    ("Empty input works without creating output", root => {
        var source=Path.Combine(root,"input");Directory.CreateDirectory(source);
        Check(Intake.Run(source,Path.Combine(root,"output"),true).Copied==0,"nonempty plan");
    }),
    ("Custom rules classify file", root => {
        var source=Input(root,"a.LOG","event");var rules=Path.Combine(root,"rules.json");File.WriteAllText(rules,"{\".log\":\"logs\"}");
        Check(Intake.Plan(source,Path.Combine(root,"output"),Intake.ReadRules(rules))[0].Category=="logs","rule missing");
    }),
    ("Unsafe categories rejected", root => {
        var rules=Path.Combine(root,"rules.json");File.WriteAllText(rules,"{\".txt\":\"../escape\"}");
        Throws<ArgumentException>(()=>Intake.ReadRules(rules));
        var source=Input(root,"a.txt","a");Throws<ArgumentException>(()=>Intake.Plan(source,Path.Combine(root,"output"),new() {[".txt"]=".."}));
    }),
    ("Duplicate extension rules rejected", root => {
        var rules = Path.Combine(root, "rules.json");
        File.WriteAllText(rules, "{\".txt\":\"documents\",\".txt\":\"other\"}");
        Throws<ArgumentException>(() => Intake.ReadRules(rules));
        File.WriteAllText(rules, "{\".txt\":\"documents\",\".TXT\":\"other\"}");
        Throws<ArgumentException>(() => Intake.ReadRules(rules));
    }),
    ("Rule categories must be strings", root => {
        var rules = Path.Combine(root, "rules.json");
        foreach (var value in new[] { "42", "null", "true", "[]" })
        {
            File.WriteAllText(rules, "{\".txt\":" + value + "}");
            Throws<ArgumentException>(() => Intake.ReadRules(rules));
        }
    }),
    ("Rules must be a JSON object", root => {
        var rules = Path.Combine(root, "rules.json");
        File.WriteAllText(rules, "[\"documents\"]");
        Throws<ArgumentException>(() => Intake.ReadRules(rules));
    }),
    ("Malformed rules rejected", root => {
        var rules=Path.Combine(root,"rules.json");File.WriteAllText(rules,"{");
        Throws<System.Text.Json.JsonException>(()=>Intake.ReadRules(rules));
    }),
    ("Verify matches copied files without changing them", root => {
        var source = Input(root, "a/report.txt", "sample content");
        Input(root, "b/report.txt", "second content");
        var target = Path.Combine(root, "output");
        Intake.Run(source, target, true);
        var first = Path.Combine(target, "documents", "a", "report.txt");
        var before = File.GetLastWriteTimeUtc(first);
        var result = Intake.Verify(source, target);
        Check(result.Mode == "verify" && result.Checked == 2 && result.Matched == 2, "incorrect verification counts");
        Check(File.GetLastWriteTimeUtc(first) == before, "verification modified a copy");
    }),
    ("Verify reports missing copied files", root => {
        var source = Input(root, "a.txt", "original");
        var target = Path.Combine(root, "output");
        Intake.Run(source, target, true);
        File.Delete(Path.Combine(target, "documents", "a.txt"));
        Throws<IOException>(() => Intake.Verify(source, target));
    }),
    ("Verify detects modified copies and changed sources", root => {
        var source = Input(root, "a.txt", "original");
        var target = Path.Combine(root, "output");
        Intake.Run(source, target, true);
        var copy = Path.Combine(target, "documents", "a.txt");
        File.WriteAllText(copy, "tampered");
        Throws<IOException>(() => Intake.Verify(source, target));
        File.WriteAllText(copy, "original");
        File.WriteAllText(Path.Combine(source, "a.txt"), "new source");
        Throws<IOException>(() => Intake.Verify(source, target));
    }),
    ("Directory symlink rejected", root => {
        var source=Input(root,"a.txt","a");var outside=Path.Combine(root,"outside");Directory.CreateDirectory(outside);
        Directory.CreateSymbolicLink(Path.Combine(source,"linked"),outside);
        Throws<IOException>(()=>Intake.Plan(source,Path.Combine(root,"output")));
    }),
    ("File symlink rejected", root => {
        var source=Input(root,"a.txt","a");File.CreateSymbolicLink(Path.Combine(source,"linked.txt"),Path.Combine(source,"a.txt"));
        Throws<IOException>(()=>Intake.Plan(source,Path.Combine(root,"output")));
    }),
    ("Symlink destination rejected", root => {
        var source=Input(root,"a.txt","a");var outside=Path.Combine(root,"outside");Directory.CreateDirectory(outside);
        var target=Path.Combine(root,"linked-output");Directory.CreateSymbolicLink(target,outside);
        Throws<IOException>(()=>Intake.Plan(source,target));
    }),
    ("Blocking parent file rejected before writes", root => {
        var source=Input(root,"a.txt","a");var target=Path.Combine(root,"output");Directory.CreateDirectory(target);File.WriteAllText(Path.Combine(target,"documents"),"block");
        Throws<IOException>(()=>Intake.Run(source,target,true));
    })
};
var failures=0;
foreach(var test in tests)
{
    // macOS /var is a system symlink; use the physical temp directory so link-rejection tests remain meaningful.
    var tempRoot = OperatingSystem.IsMacOS() ? "/private/tmp" : Path.GetTempPath();
    var root=Path.Combine(tempRoot,"intake-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
    try {test.Run(root);Console.WriteLine("PASS "+test.Name);}
    catch(Exception e) {failures++;Console.WriteLine("FAIL "+test.Name+": "+e.Message);}
    finally {Directory.Delete(root,true);}
}
Console.WriteLine($"{tests.Length-failures}/{tests.Length} tests passed");
return failures==0?0:1;
static string Input(string root,string relative,string contents)
{
    var source=Path.Combine(root,"input");var file=Path.Combine(source,relative);Directory.CreateDirectory(Path.GetDirectoryName(file)!);File.WriteAllText(file,contents);return source;
}
static void Check(bool condition,string message) {if(!condition)throw new Exception(message);}
static void Throws<T>(Action action) where T:Exception
{
    try {action();} catch(T) {return;}
    throw new Exception("Expected "+typeof(T).Name);
}
