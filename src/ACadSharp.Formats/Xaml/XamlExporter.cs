using ACadSharp.Formats.Svg;
using ACadSharp.Objects;
using ACadSharp.Tables;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACadSharp.Formats.Xaml
{
	internal class XamlExporter : ExporterBase
	{
		/// <summary>
		/// Gets or sets the configuration settings for GDFX export.
		/// </summary>
		public XamlConfiguration Configuration { get; set; } = new XamlConfiguration();

		private XamlDocumentBuilder _builder;

		/// <summary>
		/// Initializes a new instance of the <see cref="XamlExporter"/> class with the specified file path.
		/// </summary>
		/// <param name="filename">The path to the output GDFX file.</param>
		public XamlExporter(string filename)
			: this(File.Create(filename))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="XamlExporter"/> class with the specified stream.
		/// </summary>
		/// <param name="stream">The stream to write the GDFX output to.</param>
		public XamlExporter(Stream stream)
			: base(stream)
		{
		}

		/// <inheritdoc/>
		public override void Export(Layout layout)
		{
			this.createBuilder();

			this._builder.WriteLayout(layout);
		}

		/// <inheritdoc/>
		public override void Export(BlockRecord record)
		{
			this.createBuilder();

			this._builder.WriteBlock(record);
		}

		/// <inheritdoc/>
		public override void Export(CadDocument document)
		{
			this.createBuilder();

			this._builder.WriteBlock(document.ModelSpace);
		}

		private void createBuilder()
		{
			this._builder = new XamlDocumentBuilder(this.stream, this.Configuration);
			this._builder.OnNotification += this.triggerNotification;
		}
	}
}
