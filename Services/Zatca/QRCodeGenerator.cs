using System;
using System.Collections.Generic;
using System.Linq;

namespace ZatcaDotNet.Services.Zatca
{
	public sealed class QRCodeGenerator
	{
		private readonly IReadOnlyList<Tag> _tags;

		private QRCodeGenerator(IEnumerable<Tag> tags)
		{
			_tags = (tags ?? Enumerable.Empty<Tag>()).Where(t => t != null).ToList();
			if (_tags.Count == 0)
			{
				throw new ArgumentException("Malformed data structure: no valid Tag instances found.", nameof(tags));
			}
		}

		public static QRCodeGenerator CreateFromTags(IEnumerable<Tag> tags) => new QRCodeGenerator(tags);

		public byte[] EncodeTlv()
		{
			var chunks = _tags.Select(t => t.ToBytes()).ToList();
			var total = chunks.Sum(c => c.Length);
			var buffer = new byte[total];
			var offset = 0;
			foreach (var chunk in chunks)
			{
				Buffer.BlockCopy(chunk, 0, buffer, offset, chunk.Length);
				offset += chunk.Length;
			}
			return buffer;
		}

		public string EncodeBase64()
		{
			var tlv = EncodeTlv();
			return Convert.ToBase64String(tlv);
		}
	}
}


