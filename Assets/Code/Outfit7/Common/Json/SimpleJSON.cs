// Changed by Outfit7: Disable compression
//#define USE_SharpZipLib

/* * * * *
 * A simple JSON Parser / builder
 * ------------------------------
 *
 * It mainly has been written as a simple JSON parser. It can build a JSON string
 * from the node-tree, or generate a node tree from any valid JSON string.
 *
 * If you want to use compression when saving to file / stream / B64 you have to include
 * SharpZipLib ( http://www.icsharpcode.net/opensource/sharpziplib/ ) in your project and
 * define "USE_SharpZipLib" at the top of the file
 *
 * Written by Bunny83
 * 2012-06-09
 *
 * Features / attributes:
 * - provides strongly typed node classes and lists / dictionaries
 * - provides easy access to class members / array items / data values
 * - the parser ignores data types. Each value is a string.
 * - only double quotes (") are used for quoting strings.
 * - values and names are not restricted to quoted strings. They simply add up and are trimmed.
 * - There are only 3 types: arrays(JSONArray), objects(JSONClass) and values(JSONData)
 * - provides "casting" properties to easily convert to / from those types:
 *   int / float / double / bool
 * - provides a common interface for each node so no explicit casting is required.
 * - the parser try to avoid errors, but if malformed JSON is parsed the result is undefined
 *
 *
 * 2012-12-17 Update:
 * - Added internal JSONLazyCreator class which simplifies the construction of a JSON tree
 *   Now you can simple reference any item that doesn't exist yet and it will return a JSONLazyCreator
 *   The class determines the required type by it's further use, creates the type and removes itself.
 * - Added binary serialization / deserialization.
 * - Added support for BZip2 zipped binary format. Requires the SharpZipLib ( http://www.icsharpcode.net/opensource/sharpziplib/ )
 *   The usage of the SharpZipLib library can be disabled by removing or commenting out the USE_SharpZipLib define at the top
 * - The serializer uses different types when it comes to store the values. Since my data values
 *   are all of type string, the serializer will "try" which format fits best. The order is: int, float, double, bool, string.
 *   It's not the most efficient way but for a moderate amount of data it should work on all platforms.
 *
 * * * * */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;

namespace SimpleJSON
{
	public enum JSONBinaryTag
	{
		Array			= 1,
		Class			= 2,
		Value			= 3,
		IntValue		= 4,
		DoubleValue		= 5,
		BoolValue		= 6,
        FloatValue      = 7,
		LongValue		= 8,
	}

    public class JSONNode
    {
        private static readonly Queue<StringBuilder> StringBuilderPool = new Queue<StringBuilder>();

        #region common interface
        public virtual JSONNode this[int aIndex]   { get { return null; } set { } }
        public virtual JSONNode this[string aKey]  { get { return null; } set { } }
// Added by Outfit7: Get key (key: "value") this child
        public virtual string Key                  { get; protected set; }
        public virtual string Value                { get { return "";   } set { } }
		public virtual int Count                   { get { return 0;    } }

        public virtual void Add(JSONNode aItem)
        {
            Add("", aItem);
        }

// Changed by Outfit7: Set key to child item
        public virtual void Add(string aKey, JSONNode aItem)
        {
            if (aItem != null) {
                aItem.Key = aKey;
            }
        }

        public virtual JSONNode Remove(string aKey) { return null; }
        public virtual JSONNode Remove(int aIndex) { return null; }
        public virtual JSONNode Remove(JSONNode aNode) { return aNode; }

        public virtual IEnumerable<JSONNode> Childs { get { yield break;} }
        public IEnumerable<JSONNode> DeepChilds
        {
            get
            {
                foreach (var C in Childs)
                    foreach (var D in C.DeepChilds)
                        yield return D;
            }
        }

        public virtual StringBuilder ToString(StringBuilder sb)
        {
            sb.Append("JSONNode");
            return sb;
        }
        public virtual StringBuilder ToString(string aPrefix, StringBuilder sb)
        {
            sb.Append("JSONNode");
            return sb;
        }
        public override string ToString() {
            return "JSONNode";
        }
        public virtual string ToString(string aPrefix) {
            return "JSONNode";
        }

        #endregion common interface

        #region typecasting properties
        public virtual int AsInt
        {
            get
            {
                int v = 0;
// Changed by Outfit7: Consider JSON standard about numbers: http://www.json.org/
//                if (int.TryParse(Value,out v))
                if (int.TryParse(Value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out v))
                    return v;
                return 0;
            }
            set
            {
// Changed by Outfit7: Consider JSON standard about numbers: http://www.json.org/
//                Value = value.ToString();
                Value = value.ToString("D", NumberFormatInfo.InvariantInfo);
            }
        }
        public virtual long AsLong
        {
            get
            {
                long v;
                long.TryParse(Value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out v);
                return v;
            }
            set
            {
                Value = value.ToString("D", NumberFormatInfo.InvariantInfo);
            }
        }
        public virtual DateTime AsDateTime
        {
            get
            {
                try
                {
                    long b = long.Parse(Value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo);
                    return DateTime.FromBinary(b);
                }
                catch
                {
                    return DateTime.MinValue;
                }
            }
            set
            {
                // Convert to UTC to remain consistent through time zones
                // But don't touch DateTimeKind.Unspecified, which is used by MinValue & MaxValue!
                if (value.Kind == DateTimeKind.Local) {
                    value = value.ToUniversalTime();
                }
                AsLong = value.ToBinary();
            }
        }
        public virtual float AsFloat
        {
            get
            {
                float v = 0.0f;
// Changed by Outfit7: Consider JSON standard about numbers: http://www.json.org/
//                if (float.TryParse(Value,out v))
                if (float.TryParse(Value, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out v))
                    return v;
                return 0.0f;
            }
            set
            {
// Changed by Outfit7: Consider JSON standard about numbers: http://www.json.org/
//                Value = value.ToString();
                Value = value.ToString("R", NumberFormatInfo.InvariantInfo);
            }
        }
        public virtual double AsDouble
        {
            get
            {
                double v = 0.0;
// Changed by Outfit7: Consider JSON standard about numbers: http://www.json.org/
//                if (double.TryParse(Value,out v))
                if (double.TryParse(Value, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out v))
                    return v;
                return 0.0;
            }
            set
            {
// Changed by Outfit7: Consider JSON standard about numbers: http://www.json.org/
//                Value = value.ToString();
                Value = value.ToString("R", NumberFormatInfo.InvariantInfo);
            }
        }
        public virtual bool AsBool
        {
            get
            {
                bool v = false;
                if (bool.TryParse(Value,out v))
                    return v;
// Changed by Outfit7: Only "true" is a valid JSON bool
//                return !string.IsNullOrEmpty(Value);
                return false;
            }
            set
            {
                Value = (value)?"true":"false";
            }
        }
        public virtual JSONArray AsArray
        {
            get
            {
                return this as JSONArray;
            }
        }
        public virtual JSONClass AsObject
        {
            get
            {
                return this as JSONClass;
            }
        }


        #endregion typecasting properties

        #region operators
        public static implicit operator JSONNode(string s)
        {
            return new JSONData(s);
        }
        public static implicit operator string(JSONNode d)
        {
            return (d == null)?null:d.Value;
        }
		public static bool operator ==(JSONNode a, object b)
		{
			if (b == null && a is JSONLazyCreator)
				return true;
			return System.Object.ReferenceEquals(a,b);
		}
		
		public static bool operator !=(JSONNode a, object b)
		{
		    return !(a == b);
		}
		public override bool Equals (object obj)
		{
			return System.Object.ReferenceEquals(this, obj);
		}
		public override int GetHashCode ()
		{
			return base.GetHashCode();
		}


        #endregion operators

        internal static StringBuilder PopStringBuilder() {
            lock (StringBuilderPool) {
                if (StringBuilderPool.Count == 0) {
                    return new StringBuilder();
                }
                return StringBuilderPool.Dequeue();
            }
        }

        internal static void PushStringBuilder(StringBuilder sb) {
            sb.Length = 0;
            // perhaps remove capacity too?
            lock (StringBuilderPool) {
                StringBuilderPool.Enqueue(sb);
            }
        }

        internal static StringBuilder Escape(string aText, StringBuilder sb) {
            // Added by Outfit7: Fixed NRE if value is null
            if (aText == null) {
                return sb;
            }

            var len = aText.Length;
            for (int i = 0; i < len; ++i) {
                var c = aText[i];
                switch (c) {
                    case '\\': sb.Append("\\\\"); break;
                    case '\"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default: sb.Append(c); break;
                }
            }

            return sb;
        }

        internal static string Escape(string aText)
        {
            if (string.IsNullOrEmpty(aText)) {
                return string.Empty;
            }

            var sb = PopStringBuilder();
            Escape(aText, sb);
            var str = sb.ToString();
            PushStringBuilder(sb);
            return str;
        }

        public static JSONNode Parse(string aJSON)
        {
            Stack<JSONNode> stack = new Stack<JSONNode>();
            JSONNode ctx = null;
            int i = 0;
            string Token = "";
            string TokenName = "";
            bool QuoteMode = false;
            while (i < aJSON.Length)
            {
                switch (aJSON[i])
                {
                    case '{':
                        if (QuoteMode)
                        {
                            Token += aJSON[i];
                            break;
                        }
                        stack.Push(new JSONClass());
                        if (ctx != null)
                        {
                            TokenName = TokenName.Trim();
                            if (ctx is JSONArray)
                                ctx.Add(stack.Peek());
                            else if (TokenName != "")
                                ctx.Add(TokenName,stack.Peek());
                        }
                        TokenName = "";
                        Token = "";
                        ctx = stack.Peek();
                    break;

                    case '[':
                        if (QuoteMode)
                        {
                            Token += aJSON[i];
                            break;
                        }

                        stack.Push(new JSONArray());
                        if (ctx != null)
                        {
                            TokenName = TokenName.Trim();
                            if (ctx is JSONArray)
                                ctx.Add(stack.Peek());
                            else if (TokenName != "")
                                ctx.Add(TokenName,stack.Peek());
                        }
                        TokenName = "";
                        Token = "";
                        ctx = stack.Peek();
                    break;

                    case '}':
                    case ']':
                        if (QuoteMode)
                        {
                            Token += aJSON[i];
                            break;
                        }
                        if (stack.Count == 0)
                            throw new Exception("JSON Parse: Too many closing brackets");

                        stack.Pop();
                        if (Token != "")
                        {
                            TokenName = TokenName.Trim();
                            if (ctx is JSONArray)
                                ctx.Add(Token);
                            else if (TokenName != "")
                                ctx.Add(TokenName,Token);
                        }
                        TokenName = "";
                        Token = "";
                        if (stack.Count>0)
                            ctx = stack.Peek();
                    break;

                    case ':':
                        if (QuoteMode)
                        {
                            Token += aJSON[i];
                            break;
                        }
                        TokenName = Token;
                        Token = "";
                    break;

                    case '"':
                        QuoteMode ^= true;
                    break;

                    case ',':
                        if (QuoteMode)
                        {
                            Token += aJSON[i];
                            break;
                        }
                        if (Token != "")
                        {
                            if (ctx is JSONArray)
                                ctx.Add(Token);
                            else if (TokenName != "")
                                ctx.Add(TokenName, Token);
                        }
                        TokenName = "";
                        Token = "";
                    break;

                    case '\r':
                    case '\n':
                    break;

                    case ' ':
                    case '\t':
                        if (QuoteMode)
                            Token += aJSON[i];
                    break;

                    case '\\':
                        ++i;
                        if (QuoteMode)
                        {
                            char C = aJSON[i];
                            switch (C)
                            {
                                case 't' : Token += '\t'; break;
                                case 'r' : Token += '\r'; break;
                                case 'n' : Token += '\n'; break;
                                case 'b' : Token += '\b'; break;
                                case 'f' : Token += '\f'; break;
                                case 'u':
                                {
                                    string s = aJSON.Substring(i+1,4);
                                    Token += (char)int.Parse(s, System.Globalization.NumberStyles.AllowHexSpecifier);
                                    i += 4;
                                    break;
                                }
                                default  : Token += C; break;
                            }
                        }
                    break;

                    default:
                        Token += aJSON[i];
                    break;
                }
                ++i;
            }
            if (QuoteMode)
            {
                throw new Exception("JSON Parse: Quotation marks seems to be messed up.");
            }
            return ctx;
        }
		
        public virtual void Serialize(System.IO.BinaryWriter aWriter) {}

// Removed by Outfit7: Complicated and not used
//		public void SaveToStream(System.IO.Stream aData)
//		{
//			var W = new System.IO.BinaryWriter(aData);
//			Serialize(W);
//		}
//		
//		#if USE_SharpZipLib
//		public void SaveToCompressedStream(System.IO.Stream aData)
//		{
//			using (var gzipOut = new ICSharpCode.SharpZipLib.BZip2.BZip2OutputStream(aData))
//			{
//				gzipOut.IsStreamOwner = false;
//				SaveToStream(gzipOut);
//				gzipOut.Close();
//			}
//		}
//
//		public void SaveToCompressedFile(string aFileName)
//		{
//			System.IO.Directory.CreateDirectory((new System.IO.FileInfo(aFileName)).Directory.FullName);
//			using(var F = System.IO.File.OpenWrite(aFileName))
//			{
//				SaveToCompressedStream(F);
//			}
//		}
//		public string SaveToCompressedBase64()
//		{
//			using (var stream = new System.IO.MemoryStream())
//			{
//				SaveToCompressedStream(stream);
//				stream.Position = 0;
//				return System.Convert.ToBase64String(stream.ToArray());
//			}
//		}
//
//        #else
//		public void SaveToCompressedStream(System.IO.Stream aData)
//		{
//            throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
//		}
//        public void SaveToCompressedFile(string aFileName)
//        {
//            throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
//        }
//        public string SaveToCompressedBase64()
//        {
//            throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
//        }
//        #endif
//		
//		public void SaveToFile(string aFileName)
//		{
//			System.IO.Directory.CreateDirectory((new System.IO.FileInfo(aFileName)).Directory.FullName);
//			using(var F = System.IO.File.OpenWrite(aFileName))
//			{
//				SaveToStream(F);
//			}
//		}
//		public string SaveToBase64()
//		{
//			using (var stream = new System.IO.MemoryStream())
//			{
//				SaveToStream(stream);
//				stream.Position = 0;
//				return System.Convert.ToBase64String(stream.ToArray());
//			}
//		}
//		public static JSONNode Deserialize(System.IO.BinaryReader aReader)
//		{
//			JSONBinaryTag type = (JSONBinaryTag)aReader.ReadByte();
//			switch(type)
//			{
//			case JSONBinaryTag.Array:
//			{
//				int count = aReader.ReadInt32();
//				JSONArray tmp = new JSONArray();
//				for(int i = 0; i < count; i++)
//					tmp.Add(Deserialize(aReader));
//				return tmp;
//			}
//			case JSONBinaryTag.Class:
//			{
//				int count = aReader.ReadInt32();				
//				JSONClass tmp = new JSONClass();
//				for(int i = 0; i < count; i++)
//				{
//					string key = aReader.ReadString();
//					var val = Deserialize(aReader);
//					tmp.Add(key, val);
//				}
//				return tmp;
//			}
//			case JSONBinaryTag.Value:
//			{
//				return new JSONData(aReader.ReadString());
//			}
//			case JSONBinaryTag.IntValue:
//			{
//				return new JSONData(aReader.ReadInt32());
//			}
//            case JSONBinaryTag.LongValue:
//            {
//                return new JSONData(aReader.ReadInt64());
//            }
//            case JSONBinaryTag.DoubleValue:
//			{
//				return new JSONData(aReader.ReadDouble());
//			}
//			case JSONBinaryTag.BoolValue:
//			{
//				return new JSONData(aReader.ReadBoolean());
//			}
//			case JSONBinaryTag.FloatValue:
//			{
//				return new JSONData(aReader.ReadSingle());
//			}
//				
//			default:
//			{
//				throw new Exception("Error deserializing JSON. Unknown tag: " + type);
//			}
//			}
//		}
//		
//		#if USE_SharpZipLib
//		public static JSONNode LoadFromCompressedStream(System.IO.Stream aData)
//		{
//			var zin = new ICSharpCode.SharpZipLib.BZip2.BZip2InputStream(aData);
//			return LoadFromStream(zin);
//		}
//		public static JSONNode LoadFromCompressedFile(string aFileName)
//		{
//			using(var F = System.IO.File.OpenRead(aFileName))
//			{
//				return LoadFromCompressedStream(F);
//			}
//		}
//		public static JSONNode LoadFromCompressedBase64(string aBase64)
//		{
//			var tmp = System.Convert.FromBase64String(aBase64);
//			var stream = new System.IO.MemoryStream(tmp);
//			stream.Position = 0;
//			return LoadFromCompressedStream(stream);
//		}
//        #else
//   	    public static JSONNode LoadFromCompressedFile(string aFileName)
//        {
//            throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
//        }
//        public static JSONNode LoadFromCompressedStream(System.IO.Stream aData)
//        {
//            throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
//        }
//        public static JSONNode LoadFromCompressedBase64(string aBase64)
//        {
//            throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
//        }
//		#endif
//		
//		public static JSONNode LoadFromStream(System.IO.Stream aData)
//		{
//			using(var R = new System.IO.BinaryReader(aData))
//			{
//				return Deserialize(R);
//			}
//		}
//		public static JSONNode LoadFromFile(string aFileName)
//		{
//			using(var F = System.IO.File.OpenRead(aFileName))
//			{
//				return LoadFromStream(F);
//			}
//		}
//		public static JSONNode LoadFromBase64(string aBase64)
//		{
//			var tmp = System.Convert.FromBase64String(aBase64);
//			var stream = new System.IO.MemoryStream(tmp);
//			stream.Position = 0;
//			return LoadFromStream(stream);
//		}
    } // End of JSONNode

    public class JSONArray : JSONNode, IEnumerable
    {
        private List<JSONNode> m_List = new List<JSONNode>();
        public override JSONNode this[int aIndex]
        {
            get
			{
				if (aIndex<0 || aIndex >= m_List.Count)
					return new JSONLazyCreator(this);
				return m_List[aIndex];
			}
            set
            {
// Added by Outfit7: Skip null value (via implicit string operator: only JSONData with null value is null, JSONClass and JSONArray are "")
                if ((string) value == null)
                    return;
				if (aIndex<0 || aIndex >= m_List.Count)
                    m_List.Add(value);
                else
                    m_List[aIndex] = value;
            }
        }
		public override JSONNode this[string aKey]
		{
			get{ return new JSONLazyCreator(this);}
			set
            {
// Added by Outfit7: Skip null value (via implicit string operator: only JSONData with null value is null, JSONClass and JSONArray are "")
                if ((string) value == null)
                    return;
                m_List.Add(value);
            }
		}
		public override int Count
		{
			get { return m_List.Count; }
		}
        public override void Add(string aKey, JSONNode aItem)
        {
// Added by Outfit7: Skip null item (via implicit string operator: only JSONData with null value is null, JSONClass and JSONArray are "")
            if ((string) aItem == null)
                return;
// Added by Outfit7: Call base.Add
            base.Add(aKey, aItem);

            m_List.Add(aItem);
        }
        public override JSONNode Remove(int aIndex)
        {
            if (aIndex < 0 || aIndex >= m_List.Count)
                return null;
            JSONNode tmp = m_List[aIndex];
            m_List.RemoveAt(aIndex);
            return tmp;
        }
        public override JSONNode Remove(JSONNode aNode)
        {
            m_List.Remove(aNode);
            return aNode;
        }
        public override IEnumerable<JSONNode> Childs
        {
            get
            {
                foreach(JSONNode N in m_List)
                    yield return N;
            }
        }
        public IEnumerator GetEnumerator()
        {
            foreach(JSONNode N in m_List)
                yield return N;
        }
        public override StringBuilder ToString(StringBuilder sb)
        {
            sb.Append("[ ");
            int i = 0;
            foreach (JSONNode N in m_List)
            {
                if (i++ > 0)
                    sb.Append(", ");
                N.ToString(sb);
            }
            sb.Append(" ]");
            return sb;
        }
        public override StringBuilder ToString(string aPrefix, StringBuilder sb)
        {
            sb.Append("[");
            int i = 0;
            foreach (JSONNode N in m_List) {
                if (i++ > 0)
                    sb.Append(",");
                sb.Append("\n");
                sb.Append(aPrefix);
                sb.Append("  ");
                N.ToString(aPrefix + "  ", sb);
            }
            sb.Append("\n");
            sb.Append(aPrefix);
            sb.Append("]");
            return sb;
        }
        public override string ToString() {
            var stringBuilder = PopStringBuilder();
            var str = ToString(stringBuilder).ToString();
            PushStringBuilder(stringBuilder);
            return str;
        }
        public override string ToString(string aPrefix) {
            var stringBuilder = PopStringBuilder();
            var str = ToString(aPrefix, stringBuilder).ToString();
            PushStringBuilder(stringBuilder);
            return str;
        }
        public override void Serialize (System.IO.BinaryWriter aWriter)
		{
			aWriter.Write((byte)JSONBinaryTag.Array);
			aWriter.Write(m_List.Count);
			for(int i = 0; i < m_List.Count; i++)
			{
				m_List[i].Serialize(aWriter);
			}
		}
    } // End of JSONArray

    public class JSONClass : JSONNode, IEnumerable
    {
        private Dictionary<string,JSONNode> m_Dict = new Dictionary<string,JSONNode>();
        public override JSONNode this[string aKey]
        {
            get
			{
				if (m_Dict.ContainsKey(aKey))
					return m_Dict[aKey];
				else
					return new JSONLazyCreator(this, aKey);
			}
            set
            {
// Added by Outfit7: Skip null value (via implicit string operator: only JSONData with null value is null, JSONClass and JSONArray are "")
                if ((string) value == null)
                    return;
                if (m_Dict.ContainsKey(aKey))
                    m_Dict[aKey] = value;
                else
                    m_Dict.Add(aKey,value);
            }
        }
        public override JSONNode this[int aIndex]
        {
            get
            {
                if (aIndex < 0 || aIndex >= m_Dict.Count)
					return null;
                return m_Dict.ElementAt(aIndex).Value;
            }
            set
            {
// Added by Outfit7: Skip null value (via implicit string operator: only JSONData with null value is null, JSONClass and JSONArray are "")
                if ((string) value == null)
                    return;
                if (aIndex < 0 || aIndex >= m_Dict.Count)
                    return;
                string key = m_Dict.ElementAt(aIndex).Key;
                m_Dict[key] = value;
            }
        }
		public override int Count
		{
			get { return m_Dict.Count; }
		}


        public override void Add(string aKey, JSONNode aItem)
        {
// Added by Outfit7: Skip null item (via implicit string operator: only JSONData with null value is null, JSONClass and JSONArray are "")
            if ((string) aItem == null)
                return;
// Added by Outfit7: Call base.Add
            base.Add(aKey, aItem);

            if (!string.IsNullOrEmpty(aKey))
            {
                if (m_Dict.ContainsKey(aKey))
                    m_Dict[aKey] = aItem;
                else
                    m_Dict.Add(aKey, aItem);
            }
            else
                m_Dict.Add(Guid.NewGuid().ToString(), aItem);
        }

        public override JSONNode Remove(string aKey)
        {
            if (!m_Dict.ContainsKey(aKey))
                return null;
            JSONNode tmp = m_Dict[aKey];
            m_Dict.Remove(aKey);
            return tmp;
        }
        public override JSONNode Remove(int aIndex)
        {
            if (aIndex < 0 || aIndex >= m_Dict.Count)
                return null;
            var item = m_Dict.ElementAt(aIndex);
            m_Dict.Remove(item.Key);
            return item.Value;
        }
        public override JSONNode Remove(JSONNode aNode)
        {
            try
            {
                var item = m_Dict.Where(k => k.Value == aNode).First();
                m_Dict.Remove(item.Key);
                return aNode;
            }
            catch
            {
                return null;
            }
        }

        public override IEnumerable<JSONNode> Childs
        {
            get
            {
                foreach(KeyValuePair<string,JSONNode> N in m_Dict)
                    yield return N.Value;
            }
        }

        public IEnumerator GetEnumerator()
        {
            foreach(KeyValuePair<string, JSONNode> N in m_Dict)
                yield return N;
        }
        public override StringBuilder ToString(StringBuilder sb)
        {
            sb.Append("{");
            int i = 0;
            foreach (KeyValuePair<string, JSONNode> N in m_Dict)
            {
                if (i++ > 0)
                    sb.Append(", ");
                sb.Append("\"");
                Escape(N.Key, sb);
                sb.Append("\":");
                N.Value.ToString(sb);
            }
            sb.Append("}");
            return sb;
        }
        public override StringBuilder ToString(string aPrefix, StringBuilder sb)
        {
            sb.Append("{");
            int i = 0;
            foreach (KeyValuePair<string, JSONNode> N in m_Dict) {
                if (i++ > 0)
                    sb.Append(",");
                sb.Append("\n");
                sb.Append(aPrefix);
                sb.Append("  ");
                sb.Append("\"");
                Escape(N.Key, sb);
                sb.Append("\": ");
                N.Value.ToString(aPrefix + "  ", sb);
            }
            sb.Append("\n");
            sb.Append(aPrefix);
            sb.Append("}");
            return sb;
        }
        public override string ToString() {
            var stringBuilder = PopStringBuilder();
            var str = ToString(stringBuilder).ToString();
            PushStringBuilder(stringBuilder);
            return str;
        }
        public override string ToString(string aPrefix) {
            var stringBuilder = PopStringBuilder();
            var str = ToString(aPrefix, stringBuilder).ToString();
            PushStringBuilder(stringBuilder);
            return str;
        }
        public override void Serialize (System.IO.BinaryWriter aWriter)
		{
			aWriter.Write((byte)JSONBinaryTag.Class);
			aWriter.Write(m_Dict.Count);
			foreach(string K in m_Dict.Keys)
			{
				aWriter.Write(K);
				m_Dict[K].Serialize(aWriter);
			}
		}
    } // End of JSONClass

    public class JSONData : JSONNode
    {
        private string m_Data;
        public override string Value
        {
            get { return m_Data; }
            set { m_Data = value; }
        }
        public JSONData(string aData)
        {
            m_Data = aData;
        }
        public JSONData(float aData)
        {
            AsFloat = aData;
        }
        public JSONData(double aData)
        {
            AsDouble = aData;
        }
        public JSONData(bool aData)
        {
            AsBool = aData;
        }
        public JSONData(int aData)
        {
            AsInt = aData;
        }
        public JSONData(long aData)
        {
            AsLong = aData;
        }

        public override StringBuilder ToString(StringBuilder sb)
        {
            sb.Append("\"");
            Escape(m_Data, sb);
            sb.Append("\"");
            return sb;
        }
        public override StringBuilder ToString(string aPrefix, StringBuilder sb)
        {
            sb.Append("\"");
            Escape(m_Data, sb);
            sb.Append("\"");
            return sb;
        }
        public override string ToString() {
            var stringBuilder = PopStringBuilder();
            var str = ToString(stringBuilder).ToString();
            PushStringBuilder(stringBuilder);
            return str;
        }
        public override string ToString(string aPrefix) {
            var stringBuilder = PopStringBuilder();
            var str = ToString(aPrefix, stringBuilder).ToString();
            PushStringBuilder(stringBuilder);
            return str;
        }
        public override void Serialize (System.IO.BinaryWriter aWriter)
		{
			var tmp = new JSONData("");
			
			tmp.AsInt = AsInt;
			if (tmp.m_Data == this.m_Data)
			{
				aWriter.Write((byte)JSONBinaryTag.IntValue);
				aWriter.Write(AsInt);
				return;
			}
            tmp.AsLong = AsLong;
            if (tmp.m_Data == this.m_Data)
            {
                aWriter.Write((byte)JSONBinaryTag.LongValue);
                aWriter.Write(AsLong);
                return;
            }
            tmp.AsFloat = AsFloat;
			if (tmp.m_Data == this.m_Data)
			{
				aWriter.Write((byte)JSONBinaryTag.FloatValue);
				aWriter.Write(AsFloat);
				return;
			}
			tmp.AsDouble = AsDouble;
			if (tmp.m_Data == this.m_Data)
			{
				aWriter.Write((byte)JSONBinaryTag.DoubleValue);
				aWriter.Write(AsDouble);
				return;
			}

			tmp.AsBool = AsBool;
			if (tmp.m_Data == this.m_Data)
			{
				aWriter.Write((byte)JSONBinaryTag.BoolValue);
				aWriter.Write(AsBool);
				return;
			}
			aWriter.Write((byte)JSONBinaryTag.Value);
			aWriter.Write(m_Data);
		}
    } // End of JSONData
	
	internal class JSONLazyCreator : JSONNode
	{
		private JSONNode m_Node = null;
		private string m_Key = null;
		
		public JSONLazyCreator(JSONNode aNode)
		{
			m_Node = aNode;
			m_Key  = null;
		}
		public JSONLazyCreator(JSONNode aNode, string aKey)
		{
			m_Node = aNode;
			m_Key = aKey;
		}
		
		private void Set(JSONNode aVal)
		{
			if (m_Key == null)
			{
				m_Node.Add(aVal);
			}
			else
			{
				m_Node.Add(m_Key, aVal);
			}
			m_Node = null; // Be GC friendly.
		}
		
		public override JSONNode this[int aIndex]
		{
			get
			{
				return new JSONLazyCreator(this);
			}
			set
			{
				var tmp = new JSONArray();
				tmp.Add(value);
				Set(tmp);
			}
		}
			
		public override JSONNode this[string aKey]
		{
			get
			{
				return new JSONLazyCreator(this, aKey);
			}
			set
			{
				var tmp = new JSONClass();
				tmp.Add(aKey, value);
				Set(tmp);
			}
		}
		public override void Add (JSONNode aItem)
		{
			var tmp = new JSONArray();
			tmp.Add(aItem);
			Set(tmp);
		}
		public override void Add (string aKey, JSONNode aItem)
		{
			var tmp = new JSONClass();
			tmp.Add(aKey, aItem);
			Set(tmp);
		}
		public static bool operator ==(JSONLazyCreator a, object b)
		{
			if (b == null)
				return true;
			return System.Object.ReferenceEquals(a,b);
		}
		
		public static bool operator !=(JSONLazyCreator a, object b)
		{
		    return !(a == b);
		}
		public override bool Equals (object obj)
		{
			if (obj == null)
				return true;
			return System.Object.ReferenceEquals(this, obj);
		}
		public override int GetHashCode ()
		{
			return base.GetHashCode();
		}
		
        public override StringBuilder ToString(StringBuilder sb)
        {
            return sb;
        }
        public override StringBuilder ToString(string aPrefix, StringBuilder sb)
        {
            return sb;
        }
        public override string ToString() {
            var stringBuilder = PopStringBuilder();
            var str = ToString(stringBuilder).ToString();
            PushStringBuilder(stringBuilder);
            return str;
        }
        public override string ToString(string aPrefix) {
            var stringBuilder = PopStringBuilder();
            var str = ToString(aPrefix, stringBuilder).ToString();
            PushStringBuilder(stringBuilder);
            return str;
        }

        public override int AsInt
        {
            get
            {
				JSONData tmp = new JSONData(0);
				Set(tmp);
                return 0;
            }
            set
            {
				JSONData tmp = new JSONData(value);
				Set(tmp);
            }
        }
        public override long AsLong
        {
            get
            {
                JSONData tmp = new JSONData(0L);
                Set(tmp);
                return 0L;
            }
            set
            {
                JSONData tmp = new JSONData(value);
                Set(tmp);
            }
        }
        public override float AsFloat
        {
            get
            {
				JSONData tmp = new JSONData(0.0f);
				Set(tmp);
                return 0.0f;
            }
            set
            {
				JSONData tmp = new JSONData(value);
				Set(tmp);
            }
        }
        public override double AsDouble
        {
            get
            {
				JSONData tmp = new JSONData(0.0);
				Set(tmp);
                return 0.0;
            }
            set
            {
				JSONData tmp = new JSONData(value);
				Set(tmp);
            }
        }
        public override bool AsBool
        {
            get
            {
				JSONData tmp = new JSONData(false);
				Set(tmp);
                return false;
            }
            set
            {
				JSONData tmp = new JSONData(value);
				Set(tmp);
            }
        }
        public override JSONArray AsArray
        {
            get
            {
				JSONArray tmp = new JSONArray();
				Set(tmp);
                return tmp;
            }
        }
        public override JSONClass AsObject
        {
            get
            {
				JSONClass tmp = new JSONClass();
				Set(tmp);
                return tmp;
            }
        }
	} // End of JSONLazyCreator

    public static class JSON
    {
        public static JSONNode Parse(string aJSON)
        {
            return JSONNode.Parse(aJSON);
        }
    }
}