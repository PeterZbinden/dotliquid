using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DotLiquid.Exceptions;
using DotLiquid.FileSystems;
using DotLiquid.Util;

namespace DotLiquid.Tags
{
	public class Include : Block
	{
		private static readonly Regex Syntax = new Regex(string.Format(@"({0}+)(\s+(?:with|for)\s+({0}+))?", Liquid.QuotedFragment));

		private string _templateName, _variableName;
		private Dictionary<string, string> _attributes;

		public override void Initialize(string tagName, string markup, List<string> tokens)
		{
			Match syntaxMatch = Syntax.Match(markup);
			if (syntaxMatch.Success)
			{
				_templateName = syntaxMatch.Groups[1].Value;
				_variableName = syntaxMatch.Groups[3].Value;
				if (_variableName == string.Empty)
					_variableName = null;
				_attributes = new Dictionary<string, string>(Template.NamingConvention.StringComparer);
				R.Scan(markup, Liquid.TagAttributes, (key, value) => _attributes[key] = value);
			}
			else
				throw new SyntaxException("Syntax Error in tag 'case' - Valid syntax: case [condition]");

			base.Initialize(tagName, markup, tokens);
		}

		protected override void Parse(List<string> tokens)
		{
			
		}

		public override void Render(Context context, StringBuilder result)
		{
			IFileSystem fileSystem = context.Registers["file_system"] as IFileSystem ?? Template.FileSystem;
			string source = fileSystem.ReadTemplateFile(context[_templateName] as string);
			Template partial = Template.Parse(source);

			string shortenedTemplateName = _templateName.Substring(1, _templateName.Length - 2);
			object variable = context[_variableName ?? shortenedTemplateName];

			context.Stack(() =>
			{
				foreach (var keyValue in _attributes)
					context[keyValue.Key] = context[keyValue.Value];

				if (variable is IEnumerable)
				{
					((IEnumerable) variable).Cast<object>().ToList().ForEach(v =>
					{
						context[shortenedTemplateName] = v;
						result.Append(partial.Render(context));
					});
					return;
				}

				context[shortenedTemplateName] = variable;
				result.Append(partial.Render(context));
			});
		}
	}
}