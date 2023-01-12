using System.Collections.Immutable;
using HaselTweaks.InteropGenerator;
using HaselTweaks.InteropSourceGenerators.Extensions;
using HaselTweaks.InteropSourceGenerators.Models;
using LanguageExt;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HaselTweaks.InteropSourceGenerators;

[Generator]
internal sealed class MemberFunctionGenerator : IIncrementalGenerator
{
    private const string AttributeName = "FFXIVClientStructs.Interop.Attributes.MemberFunctionAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<(Validation<DiagnosticInfo, StructInfo> StructInfo,
            Validation<DiagnosticInfo, MemberFunctionInfo> MemberFunctionInfo)> structAndMemberFunctionInfos =
            context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    AttributeName,
                    static (node, _) => node is MethodDeclarationSyntax
                    {
                        Parent: StructDeclarationSyntax, AttributeLists.Count: > 0
                    },
                    static (context, _) =>
                    {
                        var structSyntax = (StructDeclarationSyntax)context.TargetNode.Parent!;

                        var methodSyntax = (MethodDeclarationSyntax)context.TargetNode;
                        var methodSymbol = (IMethodSymbol)context.TargetSymbol;

                        return (Struct: StructInfo.GetFromSyntax(structSyntax),
                            Info: MemberFunctionInfo.GetFromRoslyn(methodSyntax, methodSymbol));
                    });

        // group by struct
        IncrementalValuesProvider<(Validation<DiagnosticInfo, StructInfo> StructInfo,
            Validation<DiagnosticInfo, Seq<MemberFunctionInfo>> MemberFunctionInfos)> groupedStructInfoWithMemberInfos =
            structAndMemberFunctionInfos.TupleGroupByValidation();

        // make sure caching is working
        var structWithMemberInfos =
            groupedStructInfoWithMemberInfos.Select(static (item, _) =>
                (item.StructInfo, item.MemberFunctionInfos).Apply(static (si, mfi) =>
                    new StructWithMemberFunctionInfos(si, mfi))
            );

        context.RegisterSourceOutput(structWithMemberInfos, (sourceContext, item) =>
        {
            item.Match(
                Fail: diagnosticInfos =>
                {
                    diagnosticInfos.Iter(dInfo => sourceContext.ReportDiagnostic(dInfo.ToDiagnostic()));
                },
                Succ: structWithMemberInfo =>
                {
                    sourceContext.AddSource(structWithMemberInfo.GetFileName(), structWithMemberInfo.RenderSource());
                });
        });

        var
            collectedStructs = structWithMemberInfos.Collect();

        context.RegisterSourceOutput(collectedStructs,
            (sourceContext, structs) =>
            {
                sourceContext.AddSource("MemberFunctionGenerator.Resolver.g.cs", BuildResolverSource(structs));
            });
    }

    private static string BuildResolverSource(
        ImmutableArray<Validation<DiagnosticInfo, StructWithMemberFunctionInfos>> structInfos)
    {
        IndentedStringBuilder builder = new();

        builder.AppendLine("// <auto-generated/>");
        builder.AppendLine("using System.Runtime.CompilerServices;");
        builder.AppendLine();

        builder.AppendLine("namespace HaselTweaks.Interop;");
        builder.AppendLine();

        builder.AppendLine("public unsafe sealed partial class Resolver");
        builder.AppendLine("{");
        builder.Indent();
        builder.AppendLine("[ModuleInitializer]");
        builder.AppendLine("internal static void AddMemberFunctions()");
        builder.AppendLine("{");
        builder.Indent();

        structInfos.Iter(siv =>
            siv.IfSuccess(structInfo => structInfo.RenderResolverSource(builder)));

        builder.DecrementIndent();
        builder.AppendLine("}");
        builder.DecrementIndent();
        builder.AppendLine("}");

        return builder.ToString();
    }

    internal sealed record MemberFunctionInfo(MethodInfo MethodInfo, SignatureInfo SignatureInfo)
    {
        public static Validation<DiagnosticInfo, MemberFunctionInfo> GetFromRoslyn(
            MethodDeclarationSyntax methodSyntax, IMethodSymbol methodSymbol)
        {
            var validMethodInfo =
                MethodInfo.GetFromRoslyn(methodSyntax, methodSymbol);

            var validSignature =
                methodSymbol.GetFirstAttributeDataByTypeName(AttributeName)
                    .GetValidAttributeArgument<string>("Signature", 0, AttributeName, methodSymbol)
                    .Bind(signatureString => SignatureInfo.GetValidatedSignature(signatureString, methodSymbol));

            return (validMethodInfo, validSignature).Apply((methodInfo, signature) =>
                new MemberFunctionInfo(methodInfo, signature));
        }

        public void RenderAddress(IndentedStringBuilder builder, StructInfo structInfo)
        {
            builder.AppendLine(
                $"public static readonly Address {MethodInfo.Name} = new Address(\"{structInfo.Name}.{MethodInfo.Name}\", \"{SignatureInfo.Signature}\", {SignatureInfo.GetByteArrayString()}, {SignatureInfo.GetMaskArrayString()}, 0);");
        }

        public void RenderFunctionPointer(IndentedStringBuilder builder, StructInfo structInfo)
        {
            var thisPtrString = MethodInfo.IsStatic ? "" : structInfo.GetThisPtrTypeString();
            var fullType =
                $"delegate* unmanaged[Stdcall] <{thisPtrString}{MethodInfo.GetParameterTypeString()}{MethodInfo.ReturnType}>";
            builder.AppendLine(
                $"public static {fullType} {MethodInfo.Name} => ({fullType}) {structInfo.Name}.Addresses.{MethodInfo.Name}.Value;");
        }

        public void RenderMemberFunction(IndentedStringBuilder builder, string structName)
        {
            MethodInfo.RenderStart(builder);

            builder.AppendLine($"if (MemberFunctionPointers.{MethodInfo.Name} is null)");
            builder.Indent();
            builder.AppendLine(
                $"throw new InvalidOperationException(\"Function pointer for {structName}.{MethodInfo.Name} is null. The resolver was either uninitialized or failed to resolve address with signature {SignatureInfo.Signature}.\");");
            builder.DecrementIndent();
            builder.AppendLine();
            if (MethodInfo.IsStatic)
            {
                builder.AppendLine(
                    $"{MethodInfo.GetReturnString()}MemberFunctionPointers.{MethodInfo.Name}({MethodInfo.GetParameterNamesString()});");
            }
            else
            {
                builder.AppendLine($"fixed({structName}* thisPtr = &this)");
                builder.AppendLine("{");
                builder.Indent();
                var paramNames = MethodInfo.GetParameterNamesString();
                if (MethodInfo.Parameters.Any())
                    paramNames = ", " + paramNames;
                builder.AppendLine(
                    $"{MethodInfo.GetReturnString()}MemberFunctionPointers.{MethodInfo.Name}(thisPtr{paramNames});");
                builder.DecrementIndent();
                builder.AppendLine("}");
            }

            MethodInfo.RenderEnd(builder);
        }

        public void RenderAddToResolver(IndentedStringBuilder builder, StructInfo structInfo)
        {
            var hierarchy = structInfo.Hierarchy.Any() ? "." + string.Join(".", structInfo.Hierarchy) : "";
            var fullTypeName = "global::" + structInfo.Namespace + hierarchy + "." + structInfo.Name;
            builder.AppendLine($"Resolver.GetInstance.RegisterAddress({fullTypeName}.Addresses.{MethodInfo.Name});");
        }
    }

    private sealed record StructWithMemberFunctionInfos(StructInfo StructInfo,
        Seq<MemberFunctionInfo> MemberFunctionInfos)
    {
        public string RenderSource()
        {
            IndentedStringBuilder builder = new();

            StructInfo.RenderStart(builder);

            builder.AppendLine("public static partial class Addresses");
            builder.AppendLine("{");
            builder.Indent();
            MemberFunctionInfos.Iter(mfi => mfi.RenderAddress(builder, StructInfo));
            builder.DecrementIndent();
            builder.AppendLine("}");
            builder.AppendLine();

            builder.AppendLine("public unsafe static class MemberFunctionPointers");
            builder.AppendLine("{");
            builder.Indent();
            MemberFunctionInfos.Iter(mfi => mfi.RenderFunctionPointer(builder, StructInfo));
            builder.DecrementIndent();
            builder.AppendLine("}");

            foreach (var mfi in MemberFunctionInfos)
            {
                builder.AppendLine();
                mfi.RenderMemberFunction(builder, StructInfo.Name);
            }

            StructInfo.RenderEnd(builder);

            return builder.ToString();
        }

        public string GetFileName()
        {
            return $"{StructInfo.Namespace}.{StructInfo.Name}.MemberFunctions.g.cs";
        }

        public void RenderResolverSource(IndentedStringBuilder builder)
        {
            MemberFunctionInfos.Iter(mfi => mfi.RenderAddToResolver(builder, StructInfo));
        }
    }
}
