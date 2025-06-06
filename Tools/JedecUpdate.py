# coding: cp1252
# Version: JEDEC JEP106BL.pdf

# Example of calling this python script:
# python JedecUpdate.py JEP106BL.pdf Jedec_Updated.py

# Original: https://github.com/GlasgowEmbedded/glasgow/blob/main/software/glasgow/database/jedec.py
# No license was embedded in this file. We assume this is the BSD Zero Clause License, which is in root of the project.
# This script was copied and modified at commit 7098941a2a3c21a1927d1fc11668270dc026618d.

# Note: _manufacturers array is autogenerated
_manufacturers = [
]

def _jedec_update_mfg_from_pdf():
    import fitz
    import re
    import argparse

    parser = argparse.ArgumentParser(description="Update manufactures ID from JEDEC's official list distributed via PDF")
    parser.add_argument("pdf_filepath", default="JEP106BL.pdf", help="Offical JEP106 PDF")
    parser.add_argument("output_filepath", nargs='?', default=__file__, help="Optional output file. Default: 'jedec.py'")
    args = parser.parse_args()

    pdf_text = ''
    with fitz.open(args.pdf_filepath) as doc:
        for page in doc:
            pdf_text += page.get_text()
    jep106_matches = re.findall(r'^(\d+) (.+(?:\n.+){0,1})(?:(?:\n\d ){8})(?:\n)([0-9A-F]{2})', pdf_text, re.MULTILINE)

    output_text = ""
    with open(__file__) as source_file:
        output_text += source_file.read()

    # Update version string
    output_text = re.sub(r"^# Version.*", f"# Version: JEDEC {args.pdf_filepath}", output_text)

    manufacturers_array = "_manufacturers = [\n"
    bank_no = 0
    for entry in jep106_matches:
        entry = tuple(item.replace('\n', '').strip() for item in entry)
        if entry[0] == '1':
            bank_no += 1
            if (bank_no > 1):
                manufacturers_array += f"        }}\n    }},\n"
            manufacturers_array += f"    {{\n        {bank_no}, //Bank {bank_no}\n        new Dictionary<byte, string>\n        {{\n"
        manufacturers_array += f"            {{ 0x{int(entry[2], 16):02X}, \"{entry[1]}\" }},\n"
    manufacturers_array += "        }}\n]"

    # Update Manufacturers array
    output_text = re.sub(r"_manufacturers = \[(.|\n)*^\]", manufacturers_array, output_text, flags=re.MULTILINE)

    with open(args.output_filepath, 'w') as f:
        print(output_text, file=f, end='')

if __name__ == "__main__":
    _jedec_update_mfg_from_pdf()
