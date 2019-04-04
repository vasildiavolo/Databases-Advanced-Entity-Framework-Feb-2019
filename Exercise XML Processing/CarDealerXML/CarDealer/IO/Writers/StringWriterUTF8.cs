namespace CarDealer.IO.Writers
{
    using System.IO;
    using System.Text;

    class StringWriterUTF8 : StringWriter
    {
        public StringWriterUTF8(StringBuilder sb) : base(sb)
        {
        }

        public override Encoding Encoding => Encoding.UTF8;
    }
}
