using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Xml.XPath;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Peach.Core.Xsd
{
	#region Dom Elements

	/// <summary>
	/// Root element of a Peach XML DDL document.
	/// </summary>
	[XmlRoot(ElementName = "Peach", Namespace = Dom.TargetNamespace)]
	public class Dom
	{
		/// <summary>
		/// Namespace used by peach.
		/// </summary>
		public const string TargetNamespace = "http://peachfuzzer.com/2012/Peach";

		/// <summary>
		/// Version of this XML file.
		/// </summary>
		[XmlAttribute]
		[DefaultValue(null)]
		public string version { get; set; }

		/// <summary>
		/// Author of this XML file.
		/// </summary>
		[XmlAttribute]
		[DefaultValue(null)]
		public string author { get; set; }

		/// <summary>
		/// Description of this XML file.
		/// </summary>
		[XmlAttribute]
		[DefaultValue(null)]
		public string description { get; set; }

		[XmlElement]
		[DefaultValue(null)]
		public List<Include> Include { get; set; }

		[XmlElement]
		[DefaultValue(null)]
		public List<Import> Import { get; set; }

		[XmlElement]
		[DefaultValue(null)]
		public List<Require> Require { get; set; }

		[XmlElement]
		[DefaultValue(null)]
		public List<PythonPath> PythonPath { get; set; }

		[XmlElement]
		[DefaultValue(null)]
		public List<RubyPath> RubyPath { get; set; }

		[XmlElement]
		[DefaultValue(null)]
		public List<Python> Python { get; set; }

		[XmlElement]
		[DefaultValue(null)]
		public List<Ruby> Ruby { get; set; }

		[XmlElement]
		[DefaultValue(null)]
		public Defaults Defaults { get; set; }

		/*
		 * Ocl
		 * LangModel
		 * DataModel
		 * Data
		 * Test
		 * Agent
		 * StateModel
		 * Python
		 * Analyzer (Top Level)
		 */
	}

	/// <summary>
	/// Imports other Peach XML files into a namespace.
	/// This allows reusing existing templates from other Peach XML files.
	/// </summary>
	public class Include
	{
		/// <summary>
		/// The namespace prefix. One or more alphanumeric characters. Must not include a period.
		/// </summary>
		[XmlAttribute]
		public string ns { get; set; }

		/// <summary>
		/// URL of file to include. For files say "file:path/to/file".
		/// </summary>
		[XmlAttribute]
		public string src { get; set; }
	}

	/// <summary>
	/// Import a python file into the current context.
	/// This allows referencing generators and methods in external python files.
	/// Synonymous with saying "import xyz".
	/// </summary>
	public class Import
	{
		/// <summary>
		/// Just like the python "import xyz" syntax.
		/// </summary>
		[XmlAttribute]
		public string import { get; set; }
	}

	/// <summary>
	/// Import a ruby file into the current context.
	/// This allows referencing generators and methods in external ruby files.
	/// Synonymous with saying "require xyz".
	/// </summary>
	public class Require
	{
		/// <summary>
		/// Just like the ruby "require xyz" syntax.
		/// </summary>
		[XmlAttribute]
		public string require { get; set; }
	}

	/// <summary>
	/// Includes an additional path for python module resolution.
	/// </summary>
	public class PythonPath
	{
		/// <summary>
		/// Include this path when resolving python modules.
		/// </summary>
		[XmlAttribute]
		public string path { get; set; }
	}

	/// <summary>
	/// Includes an additional path for ruby module resolution.
	/// </summary>
	public class RubyPath
	{
		/// <summary>
		/// Include this path when resolving ruby modules.
		/// </summary>
		[XmlAttribute]
		public string path { get; set; }
	}

	/// <summary>
	/// This element allows for running Python code.
	/// This is useful to call any initialization methods for code that is later used.
	/// This is an advanced element.
	/// </summary>
	public class Python
	{
		/// <summary>
		/// Python code to run.
		/// </summary>
		[XmlAttribute]
		public string code { get; set; }
	}

	/// <summary>
	/// This element allows for running Ruby code.
	/// This is useful to call any initialization methods for code that is later used.
	/// This is an advanced element.
	/// </summary>
	public class Ruby
	{
		/// <summary>
		/// Ruby code to run.
		/// </summary>
		[XmlAttribute]
		public string code { get; set; }
	}

	/// <summary>
	/// Controls the default values of attributes for number elements.
	/// </summary>
	public class NumberDefaults
	{
		/// <summary>
		/// Specifies the byte order of the number.
		/// </summary>
		[XmlAttribute]
		[DefaultValue(Peach.Core.Dom.EndianType.Little)]
		public Peach.Core.Dom.EndianType endian { get; set; }

		/// <summary>
		/// Specifies if the the number signed.
		/// </summary>
		[XmlAttribute]
		[DefaultValue(true)]
		public bool signed { get; set; }

		/// <summary>
		/// Specifies the format of the value attribute.
		/// </summary>
		[XmlAttribute]
		[DefaultValue(Peach.Core.Dom.ValueType.String)]
		public Peach.Core.Dom.ValueType valueType { get; set; }
	}

	/// <summary>
	/// Controls the default values of attributes for string elements.
	/// </summary>
	public class StringDefaults
	{
		/// <summary>
		/// Specifies the character encoding of the string.
		/// </summary>
		[XmlAttribute]
		[DefaultValue(Peach.Core.Dom.StringType.ascii)]
		public Peach.Core.Dom.StringType type { get; set; }

		/// <summary>
		/// Specifies if the string is null terminated.
		/// </summary>
		[XmlAttribute]
		[DefaultValue(false)]
		public bool nullTerminated { get; set; }

		/// <summary>
		/// Specify the character to bad the string with if it's length if less then
		/// specified in the length attribute. Only valid when the length attribute is also
		/// specified.  This field will accept python escape sequences
		/// such as \xNN, \r, \n, etc.
		/// </summary>
		[XmlAttribute]
		[DefaultValue("\\x00")]
		public char padCharacter { get; set; }

		/// <summary>
		/// Specifies the units of the length attribute.
		/// </summary>
		[XmlAttribute]
		[DefaultValue(Peach.Core.Dom.LengthType.Bytes)]
		public Peach.Core.Dom.LengthType lengthType { get; set; }

		/// <summary>
		/// Specify the format of the value attribute.
		/// </summary>
		[XmlAttribute]
		[DefaultValue(Peach.Core.Dom.ValueType.String)]
		public Peach.Core.Dom.ValueType valueType { get; set; }
	}

	/// <summary>
	/// Controls the default values of attributes for flags elements.
	/// </summary>
	public class FlagsDefaults
	{
		/// <summary>
		/// Specifies the byte order of the flag set.
		/// </summary>
		[XmlAttribute]
		[DefaultValue(Peach.Core.Dom.EndianType.Little)]
		public Peach.Core.Dom.EndianType endian { get; set; }

		/// <summary>
		/// Specifies the length in bits of the flag set.
		/// </summary>
		[XmlAttribute]
		public uint size { get; set; }
	}

	/// <summary>
	/// Controls the default values of attributes for blob elements.
	/// </summary>
	public class BlobDefaults
	{
		/// <summary>
		/// Specifies the units of the length attribute.
		/// </summary>
		[XmlAttribute]
		[DefaultValue(Peach.Core.Dom.LengthType.Bytes)]
		public Peach.Core.Dom.LengthType lengthType { get; set; }

		/// <summary>
		/// Specifies the format of the value attribute.
		/// </summary>
		[XmlAttribute]
		[DefaultValue(Peach.Core.Dom.ValueType.String)]
		public Peach.Core.Dom.ValueType valueType { get; set; }
	}

	/// <summary>
	/// This element allow setting default values for data elements.
	/// </summary>
	public class Defaults
	{
		[XmlElement]
		[DefaultValue(null)]
		public NumberDefaults Number { get; set; }

		[XmlElement]
		[DefaultValue(null)]
		public StringDefaults String { get; set; }

		[XmlElement]
		[DefaultValue(null)]
		public FlagsDefaults Flags { get; set; }

		[XmlElement]
		[DefaultValue(null)]
		public BlobDefaults Blob { get; set; }
	}

	#endregion

	#region XmlDocFetcher

	public static class XmlDocFetcher
	{
		static Dictionary<Assembly, XPathDocument> Cache = new Dictionary<Assembly, XPathDocument>();
		static Regex Whitespace = new Regex(@"\r\n\s+", RegexOptions.Compiled);

		public static string GetSummary(Type type)
		{
			return Select(type, "T", "", "");
		}

		public static string GetSummary(PropertyInfo pi)
		{
			return Select(pi.DeclaringType, "P", pi.Name, "");
		}

		public static string GetSummary(FieldInfo fi)
		{
			return Select(fi.DeclaringType, "F", fi.Name, "");
		}

		public static string GetSummary(MethodInfo mi)
		{
			var type = mi.DeclaringType;

			var parameters = mi.GetParameters().Select(p => p.ParameterType.FullName);
			var args = string.Join(",", parameters);
			if (!string.IsNullOrEmpty(args))
				args = string.Format("({0})", args);

			return Select(type, "M", mi.Name, args);
		}

		static string Select(Type type, string prefix, string suffix, string args)
		{
			var doc = GetXmlDoc(type.Assembly);
			if (doc == null)
				return null;

			var name = string.IsNullOrEmpty(suffix) ? type.FullName : string.Join(".", type.FullName, suffix);
			var query = string.Format("/doc/members/member[@name='{0}:{1}{2}']/summary", prefix, name, args);

			var navi = doc.CreateNavigator();
			var iter = navi.Select(query);

			if (!iter.MoveNext())
				return null; // No values

			var elem = iter.Current;

			if (iter.MoveNext())
				throw new NotSupportedException(); // Multiple matches!

			var ret = Whitespace.Replace(elem.Value, "\r\n");
			return ret.Trim();
		}

		static XPathDocument GetXmlDoc(Assembly asm)
		{
			XPathDocument doc;

			if (!Cache.TryGetValue(asm, out doc))
			{
				var file = Path.ChangeExtension(asm.CodeBase, ".xml");
				doc = new XPathDocument(file);
				Cache.Add(asm, doc);
			}

			return doc;
		}
	}

	#endregion

	public static class SchemaBuilder
	{
		#region Extension Methods

		static XmlNode[] ToNodeArray(this string text)
		{
			XmlDocument doc = new XmlDocument();
			return new XmlNode[1] { doc.CreateTextNode(text) };
		}

		static void SetText(this XmlSchemaDocumentation doc, string text)
		{
			doc.Markup = text.ToNodeArray();
		}

		static void Annotate(this XmlSchemaAnnotated item, PropertyInfo pi)
		{
			item.Annotate(XmlDocFetcher.GetSummary(pi));
		}

		static void Annotate(this XmlSchemaAnnotated item, Type type)
		{
			item.Annotate(XmlDocFetcher.GetSummary(type));
		}

		static void Annotate(this XmlSchemaAnnotated item, FieldInfo fi)
		{
			item.Annotate(XmlDocFetcher.GetSummary(fi));
		}

		static void Annotate(this XmlSchemaAnnotated item, string text)
		{
			if (string.IsNullOrEmpty(text))
				return;

			var doc = new XmlSchemaDocumentation();
			doc.SetText(text);

			var anno = new XmlSchemaAnnotation();
			anno.Items.Add(doc);

			item.Annotation = anno;
		}

		#endregion

		private class Builder
		{
			Dictionary<Type, XmlSchemaSimpleType> enumTypeCache;
			XmlSchema schema;

			public Builder(Type type)
			{
				enumTypeCache = new Dictionary<Type, XmlSchemaSimpleType>();

				var root = type.GetAttributes<XmlRootAttribute>().First();

				schema = new XmlSchema();
				schema.TargetNamespace = root.Namespace;
				schema.ElementFormDefault = XmlSchemaForm.Qualified;

				AddElement(root.ElementName, type);
			}

			public XmlSchema Compile()
			{
				var schemaSet = new XmlSchemaSet();
				schemaSet.ValidationEventHandler += ValidationEventHandler;
				schemaSet.Add(schema);
				schemaSet.Compile();

				var compiled = schemaSet.Schemas().OfType<XmlSchema>().First();

				return compiled;
			}

			T MakeItem<T>(string name, Type type) where T : XmlSchemaAnnotated, new()
			{
				if (type.IsGenericType)
					throw new ArgumentException();

				var item = new T();

				var mi = typeof(T).GetProperty("Name");
				mi.SetValue(item, name, null);

				item.Annotate(type);

				// Add even though we haven't filled everything out yet
				schema.Items.Add(item);

				return item;
			}

			void AddElement(string name, Type type)
			{
				var schemaElem = MakeItem<XmlSchemaElement>(name, type);

				var complexType = new XmlSchemaComplexType();

				PopulateComplexType(complexType, type);

				schemaElem.SchemaType = complexType;
			}

			void PopulateComplexType(XmlSchemaComplexType complexType, Type type)
			{
				var schemaParticle = new XmlSchemaSequence();

				foreach (var pi in type.GetProperties())
				{
					var attrAttr = pi.GetAttributes<XmlAttributeAttribute>().FirstOrDefault();
					if (attrAttr != null)
					{
						var attr = MakeAttribute(attrAttr.AttributeName, pi);
						complexType.Attributes.Add(attr);
						continue;
					}

					var elemAttr = pi.GetAttributes<XmlElementAttribute>().FirstOrDefault();
					if (elemAttr != null)
					{
						var elem = MakeElement(elemAttr.ElementName, pi);
						schemaParticle.Items.Add(elem);
						continue;
					}
				}

				if (schemaParticle.Items.Count > 0)
					complexType.Particle = schemaParticle;
			}

			void AddComplexType(string name, Type type)
			{
				var complexType = MakeItem<XmlSchemaComplexType>(name, type);

				PopulateComplexType(complexType, type);
			}

			void ValidationEventHandler(object sender, ValidationEventArgs e)
			{
				Console.WriteLine(e.Exception);
				Console.WriteLine(e.Message);
			}

			XmlQualifiedName GetSchemaType(Type type)
			{
				if (type == typeof(char))
					return new XmlQualifiedName("string", XmlSchema.Namespace);

				if (type == typeof(string))
					return new XmlQualifiedName("string", XmlSchema.Namespace);

				if (type == typeof(bool))
					return new XmlQualifiedName("boolean", XmlSchema.Namespace);

				if (type == typeof(uint))
					return new XmlQualifiedName("unsignedInt", XmlSchema.Namespace);

				if (type == typeof(int))
					return new XmlQualifiedName("unsignedInt", XmlSchema.Namespace);

				throw new NotImplementedException();
			}

			XmlSchemaAttribute MakeAttribute(string name, PropertyInfo pi)
			{
				if (string.IsNullOrEmpty(name))
					name = pi.Name;

				var defaultValue = pi.GetAttributes<DefaultValueAttribute>().FirstOrDefault();

				var attr = new XmlSchemaAttribute();
				attr.Name = name;
				attr.Annotate(pi);

				if (pi.PropertyType.IsEnum)
				{
					attr.SchemaType = GetEnumType(pi.PropertyType);
				}
				else
				{
					attr.SchemaTypeName = GetSchemaType(pi.PropertyType);
				}

				if (defaultValue != null)
				{
					attr.Use = XmlSchemaUse.Optional;

					if (defaultValue.Value != null)
					{
						var valStr = defaultValue.Value.ToString();
						var valType = defaultValue.Value.GetType();

						if (valType == typeof(bool))
						{
							valStr = XmlConvert.ToString((bool)defaultValue.Value);
						}
						else if (valType.IsEnum)
						{
							var enumType = valType.GetField(valStr);
							var enumAttr = enumType.GetAttributes<XmlEnumAttribute>().FirstOrDefault();
							if (enumAttr != null)
								valStr = enumAttr.Name;
						}

						attr.DefaultValue = valStr;
					}
					else if (pi.PropertyType.IsEnum)
					{
						var content = (XmlSchemaSimpleTypeRestriction)attr.SchemaType.Content;
						var facet = (XmlSchemaEnumerationFacet)content.Facets[0];
						attr.DefaultValue = facet.Value;
					}
				}
				else
				{
					attr.Use = XmlSchemaUse.Required;
				}

				return attr;
			}

			XmlSchemaSimpleType GetEnumType(Type type)
			{
				XmlSchemaSimpleType ret;
				if (enumTypeCache.TryGetValue(type, out ret))
					return ret;

				var content = new XmlSchemaSimpleTypeRestriction()
				{
					BaseTypeName = new XmlQualifiedName("string", XmlSchema.Namespace),
				};

				foreach (var item in type.GetFields(BindingFlags.Static | BindingFlags.Public))
				{
					var attr = item.GetAttributes<XmlEnumAttribute>().FirstOrDefault();

					var facet = new XmlSchemaEnumerationFacet();
					facet.Value = attr != null ? attr.Name : item.Name;
					facet.Annotate(item);

					content.Facets.Add(facet);
				}

				ret = new XmlSchemaSimpleType()
				{
					Content = content,
				};

				enumTypeCache.Add(type, ret);

				return ret;
			}

			XmlSchemaElement MakeElement(string name, PropertyInfo pi)
			{
				if (string.IsNullOrEmpty(name))
					name = pi.Name;

				var type = pi.PropertyType;
				var defaultValue = pi.GetAttributes<DefaultValueAttribute>().FirstOrDefault();
				var isArray = type.IsArray;

				if (type.IsGenericType)
				{
					if (type.GetGenericTypeDefinition() != typeof(List<>))
						throw new NotSupportedException();

					var args = type.GetGenericArguments();
					if (args.Length != 1)
						throw new NotSupportedException();

					type = args[0];
					isArray = true;
				}

				var schemaElem = new XmlSchemaElement();
				schemaElem.MinOccursString = defaultValue != null ? "0" : "1";
				schemaElem.MaxOccursString = isArray ? "unbounded" : "1";

				if (name != type.Name)
				{
					schemaElem.Name = name;
					schemaElem.SchemaTypeName = new XmlQualifiedName(type.Name, schema.TargetNamespace);

					AddComplexType(type.Name, type);
				}
				else
				{
					schemaElem.RefName = new XmlQualifiedName(type.Name, schema.TargetNamespace);

					AddElement(name, type);
				}

				return schemaElem;
			}
		}

		public static void Test()
		{
			var stream = new MemoryStream();
			var settings = new XmlWriterSettings() { Indent = true, Encoding = System.Text.Encoding.UTF8 };
			var writer = XmlWriter.Create(stream, settings);
			var compiled = new Builder(typeof(Dom)).Compile();

			compiled.Write(writer);

			var buf = stream.ToArray();
			var str = Encoding.UTF8.GetString(buf);

			Console.WriteLine(str);
		}
	}
}
