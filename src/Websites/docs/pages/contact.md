# Page: /contact

## Purpose

Provide multiple contact paths: general inquiry, sales, partnership. Collect qualified leads.

---

## Breadcrumb

```
ExItS / Contact
```

---

## Forms

### General Contact Form

Fields:
- Name (required)
- Email (required)
- Subject / inquiry type (select: General, Sales, Partnership, Support)
- Message (required)
- [Submit]

### Sales Inquiry Form

Fields:
- Name (required)
- Business name
- Email (required)
- Phone (optional)
- Number of branches / business size
- Message
- [Submit]

### Partnership Form

Fields:
- Name (required)
- Organization (required)
- Email (required)
- Partnership type (select: Technology, Distribution, Reseller, Other)
- Message
- [Submit]

---

## Form Design

All forms follow the design spec:

- Dark field background
- White label above field
- Muted placeholder text
- Thin emerald/cyan border (default), emerald border (focus)
- Clear focus ring visible for keyboard users
- Accessible error messages linked to input via `aria-describedby`
- Submit button: primary gradient CTA

Desktop: 2-column layout where fields pair naturally (Name + Email side by side).
Mobile: 1-column layout.

---

## Form Submission

Submission endpoint: **TBD — WEB-D-08**

Options:
- ExItS Platform API endpoint for inquiry/contact
- Managed email/CRM service

No contact form data is stored in a Next.js database.

On success: inline success message (no redirect required).
On error: inline error message with retry guidance.

---

## Contact Information

Physical address, phone, and email: **TBD — Product Owner / legal required.**
Do not publish unverified contact details.

---

## SEO

- Title: "Contact ExItS | Sales, Partnerships, and Support"
- Description: "Get in touch with ExItS — for sales inquiries, partnerships, and general questions about the ExItS platform."
