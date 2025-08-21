using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCLargeLanguageModels.Prompting;
using RCLargeLanguageModels.Prompting.Metadata;

namespace RCLargeLanguageModels.Tests.Templates
{
	public class TemplateLibraryTests
	{
		[Fact]
		public void TemplateLibraryFullImport()
		{
			var lib = new TemplateLibrary();

			lib.ImportFromString(
			"""
			@template sample_template_another
			{
				@metadata
				{
					lang: 'en_US',
					model: 'gpt-3.5-turbo'
				}
				This is another template for GPT-3.5-Turbo model.
			}
			
			@template sample_template
			{
				@metadata
				{
					lang: 'en_US',
					model: 'gpt-3.5-turbo'
				}
				This is template for GPT-3.5-Turbo model.
			}

			@template sample_template
			{
				@metadata
				{
					lang: 'en_US',
					model: 'gpt-4'
				}
				This is template for GPT-4 model.
			}
			""");

			var template = lib.Retrieve("sample_template", new LanguageMetadata("en_US"), new TargetModelMetadata("gpt-3.5-turbo"));
			var rendered = template.Render();
			Assert.Equal("This is template for GPT-3.5-Turbo model.", rendered);

			template = lib.Retrieve("sample_template", new LanguageMetadata("en_US"), new TargetModelMetadata("gpt-4"));
			rendered = template.Render();
			Assert.Equal("This is template for GPT-4 model.", rendered);

			Assert.Throws<KeyNotFoundException>(() => lib.Retrieve("sample_template", new LanguageMetadata("fr_FR")));
		}
	}
}