using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Rename;
using System.Diagnostics.CodeAnalysis;

var solutionPath = args[0];
var translationFile = "translation.csv";

if (!MSBuildLocator.IsRegistered) MSBuildLocator.RegisterDefaults();
using var w = MSBuildWorkspace.Create();
var sln = await w.OpenSolutionAsync(solutionPath);
var classNames = new HashSet<string>();
var translation = new Dictionary<string, string>();
if (File.Exists(translationFile))
{
    using var helper = new CsvHelper.CsvReader(new StreamReader(translationFile), System.Globalization.CultureInfo.InvariantCulture);
    foreach (var item in helper.GetRecords<TranslationRecord>())
    {
        if (string.IsNullOrEmpty(item.Translation)) continue;

        translation.Add(item.OriginalName, item.Translation);
    }
}

foreach (var pid in sln.ProjectIds.ToList())
{
    // Skip test projects
    var p = sln.GetProject(pid);
    if (p.Name.Contains(".Tests.")) continue;

    foreach (var d in p.Documents)
    {
        var model = await d.GetSemanticModelAsync();
        var cus = model.SyntaxTree.GetCompilationUnitRoot();
        await Process(model, cus);
    }
}

if (!File.Exists(translationFile))
{
    using var helper = new CsvHelper.CsvWriter(new StreamWriter(translationFile), System.Globalization.CultureInfo.InvariantCulture);
    helper.WriteRecords<TranslationRecord>(classNames.Select(GetNormalizedName).Where(_ => _.Length > 1).Distinct().ToList().Order().Select(_ => new TranslationRecord(_, "")));
}

foreach (var cn in classNames.ToList().Order())
{
    Console.WriteLine(cn);
}

string GetNormalizedName(string name)
{
    if (name[0] == '_') return GetNormalizedName(name.Substring(1));
    if (char.IsLower(name[0])) return char.ToUpper(name[0]) + name.Substring(1);
    return name;
}

bool GetTranslation(string key, [NotNullWhen(true)] out string? value)
{
    if (translation.TryGetValue(key, out value))
    {
        return true;
    }

    if (char.IsLower(key[0]))
    {
        if (translation.TryGetValue(char.ToUpper(key[0]) + key.Substring(1), out value))
        {
            return true;
        }
    }

    if (key[0] == '_')
    {
        if (GetTranslation(key.Substring(1), out value))
        {
            value = "_" + value;
            return true;
        }
    }

    return false;
}

async Task Process(SemanticModel model, CompilationUnitSyntax cus)
{
    foreach (var m in cus.DescendantNodes(node => true))
    {
        if (m is ClassDeclarationSyntax)
        {
            var semanticNode = model.GetDeclaredSymbol(m);
            classNames.Add(semanticNode.Name);
            if (translation.TryGetValue(semanticNode.Name, out var translatedName))
            {
                var newSln = await Renamer.RenameSymbolAsync(sln, semanticNode, new SymbolRenameOptions() { RenameFile = true, RenameInComments = true, RenameOverloads = true }, translatedName);
                if (w.TryApplyChanges(newSln))
                {
                    sln = newSln;
                }
            }
        }
        if (m is MethodDeclarationSyntax)
        {
            var semanticNode = model.GetDeclaredSymbol(m);
            classNames.Add(semanticNode.Name);
            if (translation.TryGetValue(semanticNode.Name, out var translatedName))
            {
                sln = await Renamer.RenameSymbolAsync(sln, semanticNode, new SymbolRenameOptions() { RenameFile = true, RenameInComments = true, RenameOverloads = true }, translatedName);
                w.TryApplyChanges(sln);
            }
        }
        if (m is ParameterSyntax)
        {
            var semanticNode = model.GetDeclaredSymbol(m);
            classNames.Add(semanticNode.Name);
            if (translation.TryGetValue(semanticNode.Name, out var translatedName))
            {
                sln = await Renamer.RenameSymbolAsync(sln, semanticNode, new SymbolRenameOptions() { RenameFile = true, RenameInComments = true, RenameOverloads = true }, translatedName);
                w.TryApplyChanges(sln);
            }
        }
        if (m is PropertyDeclarationSyntax)
        {
            var semanticNode = model.GetDeclaredSymbol(m);
            classNames.Add(semanticNode.Name);
            if (translation.TryGetValue(semanticNode.Name, out var translatedName))
            {
                sln = await Renamer.RenameSymbolAsync(sln, semanticNode, new SymbolRenameOptions() { RenameFile = true, RenameInComments = true, RenameOverloads = true }, translatedName);
                w.TryApplyChanges(sln);
            }
        }
        if (m is FieldDeclarationSyntax fieldDeclarationSyntax)
        {
            foreach (var field in fieldDeclarationSyntax.Declaration.Variables)
            {
                var semanticNode = model.GetDeclaredSymbol(field);
                classNames.Add(semanticNode.Name);
                if (translation.TryGetValue(semanticNode.Name, out var translatedName))
                {
                    sln = await Renamer.RenameSymbolAsync(sln, semanticNode, new SymbolRenameOptions() { RenameFile = true, RenameInComments = true, RenameOverloads = true }, translatedName);
                    w.TryApplyChanges(sln);
                }
            }
        }
        if (m is EventDeclarationSyntax)
        {
            var semanticNode = model.GetDeclaredSymbol(m);
            classNames.Add(semanticNode.Name);
            if (translation.TryGetValue(semanticNode.Name, out var translatedName))
            {
                sln = await Renamer.RenameSymbolAsync(sln, semanticNode, new SymbolRenameOptions() { RenameFile = true, RenameInComments = true, RenameOverloads = true }, translatedName);
                w.TryApplyChanges(sln);
            }
        }
    }
}

record TranslationRecord(string OriginalName, string Translation);