# API examples
All endpoints below require a logged-in Identity session/cookie in this starter app.

- `GET /api/customers?search=Acme`
- `GET /api/customers/1`
- `POST /api/customers`
- `PUT /api/customers/1`
- `DELETE /api/customers/1` (deactivates)
- `GET /api/leads`
- `POST /api/leads`
- `GET /api/opportunities`
- `POST /api/opportunities`

Example `POST /api/customers` JSON:

```json
{
  "customerName": "Acme Retail",
  "email": "contact@acme.example",
  "phone": "9876543210",
  "companyName": "Acme Retail",
  "address": "Main Road",
  "city": "Bhubaneswar",
  "state": "Odisha"
}
```
