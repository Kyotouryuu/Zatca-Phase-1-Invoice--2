using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using ZatcaDotNet.Services.Zatca;
using QRCoder;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

static string Two(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);

static string BuildFormHtml()
{
	return @"<!DOCTYPE html>
<html lang=""ar"" dir=""rtl"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>إنشاء فاتورة ZATCA</title>
    <style>
        body { font-family: Tahoma, Arial, Helvetica, sans-serif; padding: 24px; color: #000; background: #f5f5f5; }
        .wrap { max-width: 900px; margin: 0 auto; background: white; border: 1px solid #e5e5e5; padding: 24px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        h1 { font-size: 24px; margin-bottom: 24px; color: #333; }
        .form-group { margin-bottom: 16px; }
        label { display: block; margin-bottom: 6px; font-weight: 600; color: #333; font-size: 14px; }
        input[type=""text""], input[type=""number""], textarea { width: 100%; padding: 8px 12px; border: 1px solid #ddd; border-radius: 4px; font-size: 14px; box-sizing: border-box; font-family: inherit; }
        textarea { min-height: 80px; resize: vertical; }
        .num { direction: ltr; text-align: left; }
        .items-section { margin-top: 24px; border-top: 2px solid #eee; padding-top: 24px; }
        .item-row { display: flex; gap: 12px; margin-bottom: 12px; align-items: flex-end; }
        .item-row input { flex: 1; }
        .item-row input[type=""number""] { flex: 0 0 120px; }
        .item-row input.item-price { flex: 0 0 140px; display: none; }
        .item-row.show-price input.item-price { display: block; }
        .toggle-group { display: flex; align-items: center; gap: 8px; margin-bottom: 12px; }
        .toggle-group input[type=""checkbox""] { width: auto; margin: 0; }
        .toggle-group label { margin: 0; font-weight: normal; }
        .btn-remove { padding: 8px 16px; background: #dc3545; color: white; border: none; border-radius: 4px; cursor: pointer; font-size: 14px; }
        .btn-remove:hover { background: #c82333; }
        .btn-add { padding: 8px 16px; background: #28a745; color: white; border: none; border-radius: 4px; cursor: pointer; font-size: 14px; margin-top: 8px; }
        .btn-add:hover { background: #218838; }
        .btn-submit { padding: 12px 32px; background: #007bff; color: white; border: none; border-radius: 4px; cursor: pointer; font-size: 16px; font-weight: 600; margin-top: 24px; width: 100%; }
        .btn-submit:hover { background: #0056b3; }
        .help-text { font-size: 12px; color: #666; margin-top: 4px; }
        .row { display: flex; gap: 16px; }
        .row .form-group { flex: 1; }
        /* Hide number spinners on selected fields */
        input[type=""number""].no-spin::-webkit-outer-spin-button,
        input[type=""number""].no-spin::-webkit-inner-spin-button { -webkit-appearance: none; margin: 0; }
        input[type=""number""].no-spin { -moz-appearance: textfield; }
    </style>
</head>
<body>
<div class=""wrap"">
    <h1 style=""display: flex; justify-content: space-between; align-items: center; flex-direction: row-reverse;"">
        <span>Genius Computers</span>
        <span>إنشاء فاتورة ZATCA Phase 1</span>
    </h1>
    <form method=""POST"" action=""/invoice/generate"">
        <div class=""row"">
            <div class=""form-group"">
                <label for=""seller"">البائع *</label>
                <input type=""text"" id=""seller"" name=""seller"" required value=""السادة جامعة حائل"">
            </div>
            <div class=""form-group"">
                <label for=""vat"">الرقم الضريبي *</label>
                <input type=""text"" id=""vat"" name=""vat"" required value=""301035002700003"" class=""num"">
            </div>
        </div>

        <div class=""row"">
            <div class=""form-group"" style=""flex: 1;"">
                <label for=""customer"">اسم العميل</label>
                <input type=""text"" id=""customer"" name=""customer"" placeholder=""اسم العميل"">
            </div>
            <div class=""form-group"" style=""flex: 1;"">
                <div class=""toggle-group"">
                    <input type=""checkbox"" id=""custom_tax"" name=""custom_tax"" onchange=""toggleCustomTax()"">
                    <label for=""custom_tax"">ضريبة مخصصة</label>
                </div>
                <div id=""custom_tax_percentage_container"" style=""display: none; margin-top: 8px;"">
                    <label for=""custom_tax_percentage"">نسبة الضريبة المخصصة (%)</label>
                    <input type=""number"" id=""custom_tax_percentage"" name=""custom_tax_percentage"" step=""0.01"" min=""0"" max=""100"" value=""15"" class=""num no-spin"" inputmode=""decimal"" oninput=""updateTaxRate()"">
                </div>
            </div>
        </div>

        <div class=""row"">
            <div class=""form-group"">
                <label for=""before_tax"">المبلغ قبل الضريبة *</label>
                <input type=""number"" id=""before_tax"" name=""before_tax"" step=""0.01"" class=""num no-spin"" inputmode=""decimal"" required>
            </div>
            <div class=""form-group"">
                <label for=""vat_amount"">مبلغ الضريبة *</label>
                <input type=""number"" id=""vat_amount"" name=""vat_amount"" step=""0.01"" class=""num no-spin"" inputmode=""decimal"" required>
            </div>
            <div class=""form-group"">
                <label>الإجمالي شامل الضريبة (معاينة)</label>
                <div id=""grand_total_preview"" class=""num"">—</div>
                <input type=""hidden"" id=""grand_total"" name=""grand_total"">
            </div>
        </div>

        <div class=""form-group"">
            <label for=""timestamp"">التاريخ والوقت (ISO 8601)</label>
            <input type=""text"" id=""timestamp"" name=""timestamp"" placeholder=""سيتم استخدام الوقت الحالي إذا تركت فارغاً"">
            <div class=""help-text"">مثال: 2024-01-15T10:30:00Z</div>
        </div>

        <div class=""items-section"">
            <h2 style=""font-size: 18px; margin-bottom: 16px;"">الأصناف</h2>
            <div class=""toggle-group"">
                <input type=""checkbox"" id=""show_prices"" name=""show_prices"" onchange=""togglePrices()"">
                <label for=""show_prices"">عرض أسعار الأصناف الفردية</label>
            </div>
            <div id=""items-container"">
                <div class=""item-row"">
                    <input type=""text"" name=""item_name[]"" placeholder=""اسم الصنف"" required>
                    <input type=""number"" name=""item_quantity[]"" placeholder=""الكمية"" min=""1"" required class=""num"">
                    <input type=""number"" name=""item_price[]"" placeholder=""السعر"" step=""0.01"" min=""0"" class=""num item-price no-spin"">
                    <button type=""button"" class=""btn-remove"" onclick=""removeItem(this)"">حذف</button>
                </div>
            </div>
            <button type=""button"" class=""btn-add"" onclick=""addItem()"">إضافة صنف</button>
        </div>

        <div class=""form-group"">
            <label for=""notes"">ملاحظات إضافية (اختياري)</label>
            <textarea id=""notes"" name=""notes""></textarea>
        </div>

        <button type=""submit"" class=""btn-submit"">إنشاء الفاتورة</button>
    </form>
</div>
<script>
let showPrices = false;
const DEFAULT_VAT_RATE = 0.15;
function getVatRate() {
    const customTaxCheckbox = document.getElementById('custom_tax');
    if (customTaxCheckbox && customTaxCheckbox.checked) {
        const customTaxPercentage = document.getElementById('custom_tax_percentage');
        const customRate = parseFloat(customTaxPercentage.value) || 15;
        return customRate / 100;
    }
    return DEFAULT_VAT_RATE;
}
function two(n){ return (isNaN(n) ? 0 : n).toFixed(2); }
function setPreview(value) {
    const preview = document.getElementById('grand_total_preview');
    preview.textContent = value === '' ? '—' : value;
}
function toggleCustomTax() {
    const checkbox = document.getElementById('custom_tax');
    const container = document.getElementById('custom_tax_percentage_container');
    if (checkbox.checked) {
        container.style.display = 'block';
    } else {
        container.style.display = 'none';
    }
    // Recalculate if before_tax has a value
    const beforeInput = document.getElementById('before_tax');
    if (beforeInput.value) {
        recalcFromBeforeTax();
    }
}
function updateTaxRate() {
    // Recalculate if before_tax has a value
    const beforeInput = document.getElementById('before_tax');
    if (beforeInput.value) {
        recalcFromBeforeTax();
    }
}
function recalcFromBeforeTax() {
    const beforeInput = document.getElementById('before_tax');
    const vatInput = document.getElementById('vat_amount');
    const totalHidden = document.getElementById('grand_total');
    const raw = (beforeInput.value || '').trim();
    if (raw === '') {
        totalHidden.value = '';
        setPreview('');
        return;
    }
    const before = parseFloat(raw) || 0;
    const vatRate = getVatRate();
    const vat = parseFloat((before * vatRate).toFixed(2));
    vatInput.value = two(vat);
    const total = before + vat;
    totalHidden.value = two(total);
    setPreview(two(total));
}
function recalcFromVat() {
    const beforeInput = document.getElementById('before_tax');
    const vatInput = document.getElementById('vat_amount');
    const totalHidden = document.getElementById('grand_total');
    const beforeRaw = (beforeInput.value || '').trim();
    const vatRaw = (vatInput.value || '').trim();
    if (beforeRaw === '' && vatRaw === '') {
        totalHidden.value = '';
        setPreview('');
        return;
    }
    const before = parseFloat(beforeRaw) || 0;
    const vat = parseFloat(vatRaw) || 0;
    const total = before + vat;
    totalHidden.value = two(total);
    setPreview(two(total));
}
function togglePrices() {
    showPrices = document.getElementById('show_prices').checked;
    const rows = document.querySelectorAll('.item-row');
    rows.forEach(row => {
        if (showPrices) {
            row.classList.add('show-price');
        } else {
            row.classList.remove('show-price');
        }
    });
}
function addItem() {
    const container = document.getElementById('items-container');
    const row = document.createElement('div');
    row.className = 'item-row' + (showPrices ? ' show-price' : '');
    row.innerHTML = `
        <input type=""text"" name=""item_name[]"" placeholder=""اسم الصنف"" required>
        <input type=""number"" name=""item_quantity[]"" placeholder=""الكمية"" min=""1"" required class=""num"">
        <input type=""number"" name=""item_price[]"" placeholder=""السعر"" step=""0.01"" min=""0"" class=""num item-price no-spin"">
        <button type=""button"" class=""btn-remove"" onclick=""removeItem(this)"">حذف</button>
    `;
    container.appendChild(row);
}
function removeItem(btn) {
    if (document.getElementById('items-container').children.length > 1) {
        btn.parentElement.remove();
    } else {
        alert('يجب أن يكون هناك صنف واحد على الأقل');
    }
}
const priceInputs = new WeakMap();
function handlePriceWheel(event) {
    event.preventDefault();
    const input = event.target;
    const currentValue = parseFloat(input.value) || 0;
    const delta = event.deltaY < 0 ? 1.00 : -1.00;
    const newValue = Math.max(0, currentValue + delta);
    input.value = newValue.toFixed(2);
    input.dispatchEvent(new Event('input', { bubbles: true }));
}
function setupPriceInput(input) {
    if (priceInputs.has(input)) return;
    let lastValue = parseFloat(input.value) || 0;
    priceInputs.set(input, { lastValue });
    
    input.addEventListener('input', function(e) {
        const currentValue = parseFloat(input.value) || 0;
        const data = priceInputs.get(input);
        const diff = Math.abs(currentValue - data.lastValue);
        
        if (diff > 0.005 && diff < 0.015) {
            const direction = currentValue > data.lastValue ? 1 : -1;
            const adjustedValue = data.lastValue + (direction * 1.00);
            input.value = Math.max(0, adjustedValue).toFixed(2);
            priceInputs.set(input, { lastValue: adjustedValue });
        } else {
            priceInputs.set(input, { lastValue: currentValue });
        }
    });
    
    input.addEventListener('mousedown', function(e) {
        const rect = input.getBoundingClientRect();
        const clickX = e.clientX;
        const isRTL = window.getComputedStyle(input).direction === 'rtl';
        const spinnerWidth = 20;
        
        let clickedSpinner = false;
        let isIncrement = false;
        
        if (isRTL) {
            if (clickX >= rect.left && clickX <= rect.left + spinnerWidth) {
                clickedSpinner = true;
                isIncrement = true;
            } else if (clickX >= rect.right - spinnerWidth && clickX <= rect.right) {
                clickedSpinner = true;
                isIncrement = false;
            }
        } else {
            if (clickX >= rect.right - spinnerWidth && clickX <= rect.right) {
                clickedSpinner = true;
                isIncrement = true;
            } else if (clickX >= rect.left && clickX <= rect.left + spinnerWidth) {
                clickedSpinner = true;
                isIncrement = false;
            }
        }
        
        if (clickedSpinner) {
            setTimeout(() => {
                const currentValue = parseFloat(input.value) || 0;
                const newValue = isIncrement ? currentValue + 1.00 : Math.max(0, currentValue - 1.00);
                input.value = newValue.toFixed(2);
                priceInputs.set(input, { lastValue: newValue });
                input.dispatchEvent(new Event('input', { bubbles: true }));
            }, 10);
        }
    });
}
document.addEventListener('DOMContentLoaded', function() {
    // Pricing inputs setup
    const beforeInput = document.getElementById('before_tax');
    const vatInput = document.getElementById('vat_amount');
    if (beforeInput && vatInput) {
        beforeInput.addEventListener('input', recalcFromBeforeTax);
        vatInput.addEventListener('input', recalcFromVat);
    }
});
</script>
</body>
</html>";
}

static string BuildInvoiceHtml(string seller, string vat, string subtotal, string vatAmount, string grandTotal, string qrB64, string invoiceNo, string invoiceDate, (string name, int qty, decimal? price)[] items, string? notes, string? customerName = null, bool showPrices = false)
{
	// Minimal HTML mirroring Laravel view layout; QR is embedded as base64 PNG
	return $@"
<!DOCTYPE html>
<html lang=""ar"" dir=""rtl"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>فاتورة - {invoiceNo}</title>
    <style>
        body {{ font-family: Tahoma, Arial, Helvetica, sans-serif; padding: 24px; color: #000; }}
        .wrap {{ max-width: 820px; margin: 0 auto; border: 1px solid #e5e5e5; padding: 16px; border-radius: 8px; }}
        .header {{ border-bottom: 1px solid #ddd; padding-bottom: 10px; margin-bottom: 12px; }}
        .title {{ font-size: 20px; font-weight: 700; margin-bottom: 8px; }}
        .meta-grid {{ display: flex; justify-content: space-between; gap: 20px; align-items: flex-start; }}
        .meta-column {{ display: flex; flex-direction: column; gap: 6px; flex: 0 0 auto; }}
        .meta-column.left {{ align-items: flex-end; }}
        .meta-column.right {{ align-items: flex-start; }}
        .meta-item {{ display: flex; gap: 6px; white-space: nowrap; }}
        .label {{ color: #555; font-size: 13px; font-weight: normal; }}
        .value {{ font-weight: normal; color: #333; }}
        .value.light {{ font-weight: normal; color: #333; }}
        .num {{ direction: ltr; unicode-bidi: embed; font-variant-numeric: tabular-nums; letter-spacing: .2px; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 6px; }}
        caption.desc {{ caption-side: top; text-align: right; padding: 6px 0; font-size: 14px; color: #333; }}
        th, td {{ border-bottom: 1px solid #eee; padding: 8px 6px; font-size: 14px; }}
        th {{ text-align: right; background: #fafafa; }}
        td.qty, th.qty {{ width: 120px; text-align: right; }}
        td.price, th.price {{ width: 140px; text-align: right; }}
        .totals {{ margin-top: 12px; border-top: 1px solid #eee; padding-top: 8px; }}
        .totals .row {{ margin: 4px 0; }}
        .totals .row .label {{ font-weight: 600; }}
        .qr {{ display: flex; justify-content: center; margin-top: 18px; }}
        .notes {{ white-space: pre-wrap; margin-top: 10px; line-height: 1.7; }}
    </style>
</head>
<body>
<div class=""wrap"">
    <div class=""header"">
        <div class=""title"">فاتورة ضريبية مبسطة</div>
        <div class=""meta-grid"">
            <div class=""meta-column left"">
                <div class=""meta-item""><div class=""label"">رقم الفاتورة</div><div class=""value light num"">{invoiceNo}</div></div>
                <div class=""meta-item""><div class=""label"">تاريخ الفاتورة</div><div class=""value light num"">{invoiceDate}</div></div>
            </div>
            <div class=""meta-column right"">
                {(string.IsNullOrWhiteSpace(customerName) ? "" : $@"<div class=""meta-item""><div class=""label"">اسم العميل</div><div class=""value"">{System.Net.WebUtility.HtmlEncode(customerName)}</div></div>")}
                <div class=""meta-item""><div class=""label"">البائع</div><div class=""value"">{System.Net.WebUtility.HtmlEncode(seller)}</div></div>
                <div class=""meta-item""><div class=""label"">الرقم الضريبي</div><div class=""value num"">{vat}</div></div>
            </div>
        </div>
    </div>

    <table>
        <caption class=""desc"">تفاصيل الأصناف</caption>
        <thead>
            <tr>
                <th>الصنف</th>
                <th class=""qty"">الكمية</th>
                {(showPrices ? @"<th class=""price"">السعر</th>" : "")}
            </tr>
        </thead>
        <tbody>
            {string.Join("", items.Select(it => {
                if (showPrices)
                {
                    var priceDisplay = it.price.HasValue ? Two(it.price.Value) : "-";
                    return $@"<tr><td>{System.Net.WebUtility.HtmlEncode(it.name)}</td><td class=""qty num"">{it.qty}</td><td class=""price num"">{priceDisplay}</td></tr>";
                }
                else
                {
                    return $@"<tr><td>{System.Net.WebUtility.HtmlEncode(it.name)}</td><td class=""qty num"">{it.qty}</td></tr>";
                }
            }))}
        </tbody>
    </table>

    <div class=""totals"">
        <div class=""row"">
            <div class=""label"">المجموع قبل الضريبة</div>
            <div class=""value num"">{subtotal}</div>
        </div>
        <div class=""row"">
            <div class=""label"">الضريبة</div>
            <div class=""value num"">{vatAmount}</div>
        </div>
        <div class=""row"">
            <div class=""label"">الإجمالي شامل الضريبة</div>
            <div class=""value num"">{grandTotal}</div>
        </div>
        {(string.IsNullOrWhiteSpace(notes) ? "" : $@"<div class=""notes"">{System.Net.WebUtility.HtmlEncode(notes)}</div>")}
    </div>

    <div class=""qr"">
        <img src=""data:image/png;base64,{qrB64}"" width=""240"" height=""240"" alt=""QR"" />
    </div>
</div>
</body>
</html>";
}

static string GenerateQrPngBase64(string payload)
{
	using var generator = new QRCoder.QRCodeGenerator();
	var data = generator.CreateQrCode(payload, QRCoder.QRCodeGenerator.ECCLevel.Q);
	var pngQr = new PngByteQRCode(data);
	var bytes = pngQr.GetGraphic(8);
	return Convert.ToBase64String(bytes);
}

static string BuildZatcaB64(string seller, string vat, string timestampIso, string grandTotal, string vatAmount)
{
	var tags = new[]
	{
		new Tag(1, seller),
		new Tag(2, vat),
		new Tag(3, timestampIso),
		new Tag(4, grandTotal),
		new Tag(5, vatAmount)
	};
	return ZatcaDotNet.Services.Zatca.QRCodeGenerator.CreateFromTags(tags).EncodeBase64();
}

// /dev/zatca-qr (main)
app.MapGet("/dev/zatca-qr", (HttpRequest req) =>
{
	var seller = req.Query["seller"].ToString();
	if (string.IsNullOrWhiteSpace(seller)) seller = "السادة جامعة حائل";
	var vat = req.Query["vat"].ToString();
	if (string.IsNullOrWhiteSpace(vat)) vat = "301035002700003";

	var vatAmountStr = req.Query.ContainsKey("vat_amount")
		? req.Query["vat_amount"].ToString()
		: (req.Query.ContainsKey("vat_total") ? req.Query["vat_total"].ToString() : "12939.00");
	var grandTotalStr = req.Query.ContainsKey("grand_total")
		? req.Query["grand_total"].ToString()
		: (req.Query.ContainsKey("total") ? req.Query["total"].ToString() : "99199.00");

	decimal.TryParse(grandTotalStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var grandTotal);
	decimal.TryParse(vatAmountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var vatAmount);
	var subtotal = grandTotal - vatAmount;

	var ts = req.Query["ts"].ToString();
	var timestampIso = string.IsNullOrWhiteSpace(ts)
		? DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture)
		: ts;

	var zatcaB64 = BuildZatcaB64(seller, vat, timestampIso, Two(grandTotal), Two(vatAmount));
	var qrPngB64 = GenerateQrPngBase64(zatcaB64);

	var invoiceNo = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
	var invoiceDate = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

	var items = new[]
	{
		("تأجير شاشات الكترونية حديثة جديدة لمساحة 100متر موزعة 9 أجزاء مع تصميم الإطار لكل شاشة والتمديدات والبرمجة والاشراف احتياج معرض كلية الهندسة بجامعة حائل", 9, (decimal?)null)
	};

	var html = BuildInvoiceHtml(
		seller: seller,
		vat: vat,
		subtotal: Two(subtotal),
		vatAmount: Two(vatAmount),
		grandTotal: Two(grandTotal),
		qrB64: qrPngB64,
		invoiceNo: invoiceNo,
		invoiceDate: invoiceDate,
		items: items,
		notes: null,
		showPrices: false
	);

	return Results.Content(html, "text/html; charset=utf-8");
});

// Form page
app.MapGet("/form", () => Results.Content(BuildFormHtml(), "text/html; charset=utf-8"));
app.MapGet("/invoice/form", () => Results.Redirect("/form"));

// POST endpoint to generate invoice from form
app.MapPost("/invoice/generate", async (HttpRequest req) =>
{
	var form = await req.ReadFormAsync();
	
	var customer = form["customer"].ToString();
	var seller = form["seller"].ToString();
	if (string.IsNullOrWhiteSpace(seller)) seller = "السادة جامعة حائل";
	
	var vat = form["vat"].ToString();
	if (string.IsNullOrWhiteSpace(vat)) vat = "301035002700003";

	var grandTotalStr = form["grand_total"].ToString();
	var vatAmountStr = form["vat_amount"].ToString();
	
	if (!decimal.TryParse(grandTotalStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var grandTotal))
		grandTotal = 0;
	if (!decimal.TryParse(vatAmountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var vatAmount))
		vatAmount = 0;
	
	var subtotal = grandTotal - vatAmount;

	var timestampIso = form["timestamp"].ToString();
	if (string.IsNullOrWhiteSpace(timestampIso))
		timestampIso = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture);

	// Check if prices should be shown
	var showPrices = form.ContainsKey("show_prices") && form["show_prices"].ToString().Equals("on", StringComparison.OrdinalIgnoreCase);
	
	// Parse items
	var itemNames = form["item_name[]"].ToArray();
	var itemQuantities = form["item_quantity[]"].ToArray();
	var itemPrices = form["item_price[]"].ToArray();
	var items = new List<(string name, int qty, decimal? price)>();
	
	for (int i = 0; i < itemNames.Length; i++)
	{
		var name = itemNames[i].ToString();
		if (string.IsNullOrWhiteSpace(name)) continue;
		
		var qtyStr = itemQuantities[i].ToString();
		if (int.TryParse(qtyStr, out var qty) && qty > 0)
		{
			decimal? price = null;
			if (showPrices && i < itemPrices.Length)
			{
				var priceStr = itemPrices[i].ToString();
				if (!string.IsNullOrWhiteSpace(priceStr) && decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedPrice) && parsedPrice > 0)
				{
					price = parsedPrice;
				}
			}
			items.Add((name, qty, price));
		}
	}
	
	if (items.Count == 0)
	{
		items.Add(("صنف غير محدد", 1, null));
	}

	var notes = form["notes"].ToString();

	var zatcaB64 = BuildZatcaB64(seller, vat, timestampIso, Two(grandTotal), Two(vatAmount));
	var qrPngB64 = GenerateQrPngBase64(zatcaB64);

	var invoiceNo = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
	var invoiceDate = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

	var html = BuildInvoiceHtml(
		seller: seller,
		vat: vat,
		subtotal: Two(subtotal),
		vatAmount: Two(vatAmount),
		grandTotal: Two(grandTotal),
		qrB64: qrPngB64,
		invoiceNo: invoiceNo,
		invoiceDate: invoiceDate,
		items: items.ToArray(),
		notes: notes,
		customerName: string.IsNullOrWhiteSpace(customer) ? null : customer,
		showPrices: showPrices
	);

	return Results.Content(html, "text/html; charset=utf-8");
});

// Root redirect
app.MapGet("/", () => Results.Redirect("/form"));

// Aliases
app.MapGet("/dev/qr", () => Results.Redirect("/dev/zatca-qr"));

// /dev/zatca-qr-2 (second invoice)
app.MapGet("/dev/zatca-qr-2", (HttpRequest req) =>
{
	var seller = req.Query["seller"].ToString();
	if (string.IsNullOrWhiteSpace(seller)) seller = "السادة جامعة حائل";
	var vat = req.Query["vat"].ToString();
	if (string.IsNullOrWhiteSpace(vat)) vat = "301035002700003";

	var vatAmountStr = req.Query.ContainsKey("vat_amount") ? req.Query["vat_amount"].ToString() : "4500";
	var grandTotalStr = req.Query.ContainsKey("grand_total") ? req.Query["grand_total"].ToString() : "34500";
	decimal.TryParse(grandTotalStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var grandTotal);
	decimal.TryParse(vatAmountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var vatAmount);
	var subtotal = grandTotal - vatAmount;

	var ts = req.Query["ts"].ToString();
	var timestampIso = string.IsNullOrWhiteSpace(ts)
		? DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture)
		: ts;

	var zatcaB64 = BuildZatcaB64(seller, vat, timestampIso, Two(grandTotal), Two(vatAmount));
	var qrPngB64 = GenerateQrPngBase64(zatcaB64);

	var invoiceNo = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
	var invoiceDate = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

	var items = new[]
	{
		("كراسي انتظار", 15, (decimal?)null),
		("مكتب خاص صغير ذو جودة عالية", 2, (decimal?)null),
		("كراسي محطات عمل", 24, (decimal?)null)
	};

	var html = BuildInvoiceHtml(
		seller: seller,
		vat: vat,
		subtotal: Two(subtotal),
		vatAmount: Two(vatAmount),
		grandTotal: Two(grandTotal),
		qrB64: qrPngB64,
		invoiceNo: invoiceNo,
		invoiceDate: invoiceDate,
		items: items,
		notes: null,
		showPrices: false
	);

	return Results.Content(html, "text/html; charset=utf-8");
});

// Alias
app.MapGet("/dev/qr2", () => Results.Redirect("/dev/zatca-qr-2"));

app.Run();


