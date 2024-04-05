namespace dotnet_core_web_client.Utilities
{
	/// <summary>
	/// background: when a new device is first powered up, it will fetch a serial number from the photo.iguardpayroll.com,
	/// and jacky will return a sequencially increasing number, and the device will convert it to uint and saved it for
	/// later use, using the same algorithm below. So when the device sends an accessLog to iGuardPayroll, it only includes
	/// the string serial number, but will send the uint to the photo server. That's why I need to convert the string to uint
	/// when trying to get the pictures of a particular accessLog from the photo server. (240403)
	/// </summary>
	public class SerialNoConverter
	{
		// both of these two functions are written by Jacky with slight modification (refer to whatsapp) (240328)
		public static uint StringToUint(string sn1)
		{
			uint rc;
			uint cs;
			uint prefix;
			uint prefix2;

			string sn = sn1.Replace("-", "");
			cs = sn[sn.Length - 1];                 // last digit is checksum
			prefix = sn[0];                         // first digit prefix
			prefix2 = sn[1];
			sn = sn.Substring(2, sn.Length - 3);    // remove first & 2nd and last digit

			rc = uint.Parse(sn);

			rc = (rc << 4) & 0xFFFFFF;   // shift one digit and remove MS digit
			rc |= cs - '0';
			rc |= (prefix - '0') << 28;
			rc |= (prefix2 - '0') << 24;

			return rc;
		}

		public static string UintToString(uint hsn)
		{
			// first assume a two-digit prefix (240404)
			uint p = (hsn & 0xFF000000) >> 24;  // Prefix
			uint n = (hsn & 0x00FFFFFF) >> 4;   // SN
			uint cs = hsn & 0xF;                // Checksum
			string s2 = $"{p:X2}{n:D9}{cs}";    // Prefix + SN + CS

			s2 = s2.Insert(8, "-");
			s2 = s2.Insert(4, "-");

			if (IsValidSerialNo(s2))
			{
				return s2;
			}

			// now it should be a one-digit prefix (240404)
			p = (hsn & 0xF0000000) >> 28;	// Prefix
			n = (hsn & 0x0FFFFFFF) >> 4;	// SN
			cs = hsn & 0xF;					// Checksum
			string s = $"{p}{n:D10}{cs}";	// Prefix + SN + CS

			s = s.Insert(8, "-");
			s = s.Insert(4, "-");

			return s;
		}

		public static bool IsValidSerialNo(string sn)
		{
			int cs = 0;
			int n = 2;
			int dashes = 0;
			int last = 0;
			int len = sn.Length;
			int start = 2;

			if (len < 12)
			{
				return false;
			}

			cs = 7;

			for (int i = start; i < len; i++)
			{
				char ch = sn[i];

				if (char.IsDigit(ch))
				{
					last = ch - '0';
					cs += last;
					n++;
				}
				else if (ch != '-' || ++dashes > 2)
				{
					return false;
				}
			}

			if (n != 12 || dashes != 2)
			{
				return false;
			}

			cs = (cs - last) % 10;

			return (cs == last);
		}
	}
}
