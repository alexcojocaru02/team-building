export default async function globalSetup() {
  // E2E_ADMIN user must already exist in the database with Admin role.
  // Set it manually via: db.Users.updateOne({ email: "e2e-admin@teamconnect.test" }, { $set: { role: "Admin" } })
}
