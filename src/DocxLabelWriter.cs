using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace HatchGrouper
{
    /// <summary>Generates clean OpenXML Word document (.docx) with printable installation labels.</summary>
    public static class DocxLabelWriter
    {
        public static void Export(string filePath, IEnumerable<NicheDetailRow> items, LengthUnit unit)
        {
            if (File.Exists(filePath)) File.Delete(filePath);

            using var zip = ZipFile.Open(filePath, ZipArchiveMode.Create);

            AddZipEntry(zip, "[Content_Types].xml", ContentTypesXml);
            AddZipEntry(zip, "_rels/.rels", RootRelsXml);
            AddZipEntry(zip, "word/_rels/document.xml.rels", DocRelsXml);
            AddZipEntry(zip, "word/styles.xml", StylesXml);
            AddZipEntry(zip, "word/document.xml", BuildDocumentXml(items, unit));
        }

        private static void AddZipEntry(ZipArchive zip, string entryName, string content)
        {
            var entry = zip.CreateEntry(entryName, CompressionLevel.Optimal);
            using var stream = entry.Open();
            using var writer = new StreamWriter(stream, Encoding.UTF8);
            writer.Write(content);
        }

        private static string BuildDocumentXml(IEnumerable<NicheDetailRow> items, LengthUnit unit)
        {
            var list = items?.ToList() ?? new List<NicheDetailRow>();
            string u = UnitHelper.Name(unit);

            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n");
            sb.Append("<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">\n");
            sb.Append("  <w:body>\n");

            // Title
            sb.Append("    <w:p>\n");
            sb.Append("      <w:pPr><w:jc w:val=\"center\"/><w:spacing w:before=\"0\" w:after=\"140\"/></w:pPr>\n");
            sb.Append("      <w:r><w:rPr><w:b/><w:sz w:val=\"26\"/><w:color w:val=\"0F172A\"/></w:rPr>");
            sb.Append($"<w:t>{Escape(Loc.T("labels.docx.title"))}</w:t></w:r>\n");
            sb.Append("    </w:p>\n");

            // 2-column table
            sb.Append("    <w:tbl>\n");
            sb.Append("      <w:tblPr>\n");
            sb.Append("        <w:tblW w:w=\"10400\" w:type=\"dxa\"/>\n");
            sb.Append("        <w:jc w:val=\"center\"/>\n");
            sb.Append("        <w:tblCellMar>\n");
            sb.Append("          <w:top w:w=\"80\" w:type=\"dxa\"/><w:bottom w:w=\"80\" w:type=\"dxa\"/>\n");
            sb.Append("          <w:left w:w=\"100\" w:type=\"dxa\"/><w:right w:w=\"100\" w:type=\"dxa\"/>\n");
            sb.Append("        </w:tblCellMar>\n");
            sb.Append("      </w:tblPr>\n");
            sb.Append("      <w:tblGrid>\n");
            sb.Append("        <w:gridCol w:w=\"5200\"/>\n");
            sb.Append("        <w:gridCol w:w=\"5200\"/>\n");
            sb.Append("      </w:tblGrid>\n");

            for (int i = 0; i < list.Count; i += 2)
            {
                var left = list[i];
                var right = (i + 1 < list.Count) ? list[i + 1] : null;

                sb.Append("      <w:tr>\n");
                sb.Append("        <w:trPr><w:cantSplit/></w:trPr>\n");

                sb.Append(BuildCellXml(left, u));
                sb.Append(BuildCellXml(right, u));

                sb.Append("      </w:tr>\n");
            }

            sb.Append("    </w:tbl>\n");

            // Section properties: A4, 0.5 in margins
            sb.Append("    <w:sectPr>\n");
            sb.Append("      <w:pgSz w:w=\"11906\" w:h=\"16838\"/>\n");
            sb.Append("      <w:pgMar w:top=\"720\" w:right=\"720\" w:bottom=\"720\" w:left=\"720\"/>\n");
            sb.Append("    </w:sectPr>\n");

            sb.Append("  </w:body>\n");
            sb.Append("</w:document>");
            return sb.ToString();
        }

        private static string BuildCellXml(NicheDetailRow item, string unit)
        {
            if (item == null)
            {
                // Blank empty cell to keep grid alignment
                return "        <w:tc><w:tcPr><w:tcW w:w=\"5200\" w:type=\"dxa\"/><w:tcBorders><w:top w:val=\"none\"/><w:bottom w:val=\"none\"/><w:left w:val=\"none\"/><w:right w:val=\"none\"/></w:tcBorders></w:tcPr><w:p/></w:tc>\n";
            }

            var sb = new StringBuilder();
            sb.Append("        <w:tc>\n");
            sb.Append("          <w:tcPr>\n");
            sb.Append("            <w:tcW w:w=\"5200\" w:type=\"dxa\"/>\n");
            sb.Append("            <w:tcBorders>\n");
            sb.Append("              <w:top w:val=\"single\" w:sz=\"6\" w:space=\"0\" w:color=\"94A3B8\"/>\n");
            sb.Append("              <w:bottom w:val=\"single\" w:sz=\"6\" w:space=\"0\" w:color=\"94A3B8\"/>\n");
            sb.Append("              <w:left w:val=\"single\" w:sz=\"6\" w:space=\"0\" w:color=\"94A3B8\"/>\n");
            sb.Append("              <w:right w:val=\"single\" w:sz=\"6\" w:space=\"0\" w:color=\"94A3B8\"/>\n");
            sb.Append("            </w:tcBorders>\n");
            sb.Append("            <w:shd w:val=\"clear\" w:color=\"auto\" w:fill=\"F8FAFC\"/>\n");
            sb.Append("            <w:tcMar>\n");
            sb.Append("              <w:top w:w=\"100\" w:type=\"dxa\"/><w:bottom w:w=\"100\" w:type=\"dxa\"/>\n");
            sb.Append("              <w:left w:w=\"140\" w:type=\"dxa\"/><w:right w:w=\"140\" w:type=\"dxa\"/>\n");
            sb.Append("            </w:tcMar>\n");
            sb.Append("          </w:tcPr>\n");

            // Header: Entrance & Floor
            sb.Append("          <w:p>\n");
            sb.Append("            <w:pPr><w:jc w:val=\"center\"/><w:spacing w:before=\"40\" w:after=\"40\"/></w:pPr>\n");
            sb.Append("            <w:r><w:rPr><w:b/><w:sz w:val=\"26\"/><w:color w:val=\"0369A1\"/></w:rPr>");
            sb.Append($"<w:t>{Escape(Loc.T("labels.field.entrance"))}: {item.Entrance}    {Escape(Loc.T("labels.field.floor"))}: {item.NicheNo}</w:t></w:r>\n");
            sb.Append("            <w:r><w:rPr><w:b/><w:sz w:val=\"20\"/><w:color w:val=\"64748B\"/></w:rPr>");
            sb.Append($"<w:t xml:space=\"preserve\">   {Escape(item.Id)}</w:t></w:r>\n");
            sb.Append("          </w:p>\n");

            // Box Size
            sb.Append("          <w:p>\n");
            sb.Append("            <w:pPr><w:spacing w:before=\"30\" w:after=\"20\"/></w:pPr>\n");
            sb.Append("            <w:r><w:rPr><w:b/><w:sz w:val=\"22\"/><w:color w:val=\"0F172A\"/></w:rPr>");
            sb.Append($"<w:t>{Escape(Loc.T("labels.field.box"))}: </w:t></w:r>\n");
            sb.Append("            <w:r><w:rPr><w:b/><w:sz w:val=\"24\"/><w:color w:val=\"0284C7\"/></w:rPr>");
            sb.Append($"<w:t>{Escape(item.BoxDrawing)} {Escape(Loc.T("unit.mm"))}</w:t></w:r>\n");
            if (!string.IsNullOrEmpty(item.Box) && item.Box != item.BoxDrawing)
            {
                sb.Append("            <w:r><w:rPr><w:sz w:val=\"18\"/><w:color w:val=\"64748B\"/></w:rPr>");
                sb.Append($"<w:t xml:space=\"preserve\">  ({Escape(Loc.T("labels.field.exact"))}: {Escape(item.Box)} {Escape(unit)})</w:t></w:r>\n");
            }
            sb.Append("          </w:p>\n");

            // Niche Size
            sb.Append("          <w:p>\n");
            sb.Append("            <w:pPr><w:spacing w:before=\"20\" w:after=\"20\"/></w:pPr>\n");
            sb.Append("            <w:r><w:rPr><w:sz w:val=\"20\"/><w:color w:val=\"334155\"/></w:rPr>");
            sb.Append($"<w:t>{Escape(Loc.T("labels.field.niche"))}: </w:t></w:r>\n");
            sb.Append("            <w:r><w:rPr><w:b/><w:sz w:val=\"20\"/><w:color w:val=\"0F172A\"/></w:rPr>");
            sb.Append($"<w:t>{Escape(item.Niche)} {Escape(unit)}</w:t></w:r>\n");
            sb.Append("          </w:p>\n");

            // Hatch Door & Trim
            sb.Append("          <w:p>\n");
            sb.Append("            <w:pPr><w:spacing w:before=\"20\" w:after=\"20\"/></w:pPr>\n");
            sb.Append("            <w:r><w:rPr><w:sz w:val=\"18\"/><w:color w:val=\"475569\"/></w:rPr>");
            sb.Append($"<w:t>{Escape(Loc.T("labels.field.hatch"))}: {Escape(item.Hatch)} {Escape(unit)}   |   {Escape(Loc.T("labels.field.trim"))}: {Escape(item.WithTrim)} {Escape(unit)}</w:t></w:r>\n");
            sb.Append("          </w:p>\n");

            // Group & Note
            sb.Append("          <w:p>\n");
            sb.Append("            <w:pPr><w:spacing w:before=\"20\" w:after=\"40\"/></w:pPr>\n");
            sb.Append("            <w:r><w:rPr><w:b/><w:sz w:val=\"18\"/><w:color w:val=\"0F766E\"/></w:rPr>");
            sb.Append($"<w:t>{Escape(item.GroupName)}</w:t></w:r>\n");
            if (!string.IsNullOrWhiteSpace(item.Note))
            {
                sb.Append("            <w:r><w:rPr><w:b/><w:sz w:val=\"18\"/><w:color w:val=\"B45309\"/></w:rPr>");
                sb.Append($"<w:t xml:space=\"preserve\">   [{Escape(Loc.T("labels.field.note"))}: {Escape(item.Note)}]</w:t></w:r>\n");
            }
            sb.Append("          </w:p>\n");

            sb.Append("        </w:tc>\n");
            return sb.ToString();
        }

        private static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("&", "&amp;")
                       .Replace("<", "&lt;")
                       .Replace(">", "&gt;")
                       .Replace("\"", "&quot;")
                       .Replace("'", "&apos;");
        }

        private const string ContentTypesXml = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">
  <Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>
  <Default Extension=""xml"" ContentType=""application/xml""/>
  <Override PartName=""/word/document.xml"" ContentType=""application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml""/>
  <Override PartName=""/word/styles.xml"" ContentType=""application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml""/>
</Types>";

        private const string RootRelsXml = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""word/document.xml""/>
</Relationships>";

        private const string DocRelsXml = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"" Target=""styles.xml""/>
</Relationships>";

        private const string StylesXml = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<w:styles xmlns:w=""http://schemas.openxmlformats.org/wordprocessingml/2006/main"">
  <w:docDefaults>
    <w:rPrDefault>
      <w:rPr>
        <w:rFonts w:ascii=""Calibri"" w:hAnsi=""Calibri"" w:cs=""Calibri""/>
        <w:sz w:val=""20""/>
        <w:szCs w:val=""20""/>
        <w:lang w:val=""uk-UA""/>
      </w:rPr>
    </w:rPrDefault>
  </w:docDefaults>
</w:styles>";
    }
}
