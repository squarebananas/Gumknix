using System;

namespace Gumknix
{
    public struct FileExtensionFilter
    {
        public string Description;
        public string[] Extensions;

        public FileExtensionFilter(string description, string[] extensions)
        {
            Description = description;
            Extensions = extensions;
        }

        public override string ToString()
        {
            return $"{Description} ({string.Join(", ", Extensions)})";
        }
    }
}
