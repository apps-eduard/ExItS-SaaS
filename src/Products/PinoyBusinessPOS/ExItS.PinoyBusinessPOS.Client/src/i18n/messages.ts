export type MessageKey =
  | "app.name"
  | "app.skipToContent"
  | "shell.settings"
  | "shell.back"
  | "nav.personal"
  | "nav.home"
  | "nav.people"
  | "nav.invitations"
  | "nav.notifications"
  | "home.title"
  | "home.tagline"
  | "home.welcome"
  | "home.openPersonal"
  | "empty.title"
  | "empty.body"
  | "loading.label"
  | "error.title"
  | "error.body"
  | "error.reset"
  | "diagnostics.copy"
  | "diagnostics.copied"
  | "theme.label"
  | "theme.system"
  | "theme.light"
  | "theme.dark"
  | "locale.label"
  | "locale.en"
  | "locale.filPH"
  | "appearance.title"
  | "pwa.updateAvailable"
  | "pwa.refresh"
  | "auth.checking"
  | "auth.signInTitle"
  | "auth.signInSubtitle"
  | "auth.username"
  | "auth.password"
  | "auth.showPassword"
  | "auth.hidePassword"
  | "auth.submit"
  | "auth.invalidCredentials"
  | "auth.signInFailed"
  | "auth.rateLimited"
  | "auth.signedInAs"
  | "auth.signOut"
  | "people.title"
  | "people.add"
  | "people.addPerson"
  | "people.search"
  | "people.searchPlaceholder"
  | "people.emptyTitle"
  | "people.emptyBody"
  | "people.loadError"
  | "people.localContact"
  | "people.status.notConnected"
  | "people.status.requestPending"
  | "people.status.connected"
  | "people.add.lede"
  | "people.add.scanQr"
  | "people.add.scanHint"
  | "people.add.exitsId"
  | "people.add.exitsIdPlaceholder"
  | "people.add.find"
  | "people.add.requiredId"
  | "people.add.notFound"
  | "people.add.cannotAddSelf"
  | "people.add.identityFound"
  | "people.add.cancel"
  | "people.add.confirm"
  | "people.detail.notFoundTitle"
  | "people.detail.notFoundBody"
  | "people.detail.notConnectedHelp"
  | "people.detail.waitingTitle"
  | "people.detail.waitingBody"
  | "people.detail.sentOn"
  | "people.detail.cancelRequest"
  | "people.detail.sendAgain"
  | "people.detail.utang"
  | "people.detail.iLent"
  | "people.detail.iBorrowed"
  | "people.detail.amount"
  | "people.detail.amountInvalid"
  | "people.detail.confirmUtang"
  | "people.detail.relationship"
  | "people.detail.connectedSince"
  | "invitations.title"
  | "invitations.received"
  | "invitations.sent"
  | "invitations.emptyTitle"
  | "invitations.emptyBody"
  | "invitations.sentEmpty"
  | "invitations.personalUtangRequest"
  | "invitations.waitingResponse"
  | "invitations.someone"
  | "invitations.respondTitle"
  | "invitations.respondHelp"
  | "invitations.tokenLabel"
  | "invitations.tokenRequired"
  | "invitations.accept"
  | "invitations.decline"
  | "notifications.title"
  | "notifications.emptyTitle"
  | "notifications.emptyBody"
  | "notifications.unread"
  | "notifications.markRead";

export const en: Record<MessageKey, string> = {
  "app.name": "ExItS Mobile",
  "app.skipToContent": "Skip to content",
  "shell.settings": "Settings",
  "shell.back": "Back",
  "nav.personal": "Personal navigation",
  "nav.home": "Home",
  "nav.people": "People",
  "nav.invitations": "Invitations",
  "nav.notifications": "Alerts",
  "home.title": "ExItS Mobile",
  "home.tagline":
    "Your business and personal ExItS experience, designed for phone, tablet, and desktop.",
  "home.welcome": "Welcome,",
  "home.openPersonal": "Open Personal",
  "empty.title": "Nothing to show yet",
  "empty.body": "There is nothing to display right now.",
  "loading.label": "Loading",
  "error.title": "Something went wrong",
  "error.body": "Unable to complete this operation.",
  "error.reset": "Try again",
  "diagnostics.copy": "Copy",
  "diagnostics.copied": "Copied",
  "theme.label": "Theme",
  "theme.system": "System",
  "theme.light": "Light",
  "theme.dark": "Dark",
  "locale.label": "Language",
  "locale.en": "English",
  "locale.filPH": "Filipino",
  "appearance.title": "Appearance",
  "pwa.updateAvailable": "New version available",
  "pwa.refresh": "Refresh",
  "auth.checking": "Checking session",
  "auth.signInTitle": "Sign in",
  "auth.signInSubtitle": "Use your ExItS account.",
  "auth.username": "Email or username",
  "auth.password": "Password",
  "auth.showPassword": "Show password",
  "auth.hidePassword": "Hide password",
  "auth.submit": "Sign in",
  "auth.invalidCredentials": "Email or password is incorrect.",
  "auth.signInFailed": "Unable to sign in. Try again.",
  "auth.rateLimited": "Too many sign-in attempts. Try again later.",
  "auth.signedInAs": "Signed in as",
  "auth.signOut": "Sign out",
  "people.title": "People",
  "people.add": "Add",
  "people.addPerson": "Add person",
  "people.search": "Search people",
  "people.searchPlaceholder": "Search people...",
  "people.emptyTitle": "No people yet",
  "people.emptyBody":
    "Add someone you lend to, borrow from, or want to keep track of.",
  "people.loadError": "Unable to load people.",
  "people.localContact": "Local contact",
  "people.status.notConnected": "Not connected",
  "people.status.requestPending": "Request pending",
  "people.status.connected": "Connected",
  "people.add.lede": "Find someone with their exact ExItS ID or QR payload.",
  "people.add.scanQr": "Scan QR",
  "people.add.scanHint": "Paste the QR payload into ExItS ID, then find the person.",
  "people.add.exitsId": "ExItS ID",
  "people.add.exitsIdPlaceholder": "EX-____-____",
  "people.add.find": "Find person",
  "people.add.requiredId": "Enter an exact ExItS ID.",
  "people.add.notFound": "No matching ExItS identity was found.",
  "people.add.cannotAddSelf": "You cannot add yourself.",
  "people.add.identityFound": "Identity found",
  "people.add.cancel": "Cancel",
  "people.add.confirm": "Add person",
  "people.detail.notFoundTitle": "Person not found",
  "people.detail.notFoundBody": "This person is not in your People list.",
  "people.detail.notConnectedHelp":
    "You can keep this person in People without sharing Utang activity.",
  "people.detail.waitingTitle": "Request pending",
  "people.detail.waitingBody": "Waiting for {name} to respond.",
  "people.detail.sentOn": "Sent {date}",
  "people.detail.cancelRequest": "Cancel request",
  "people.detail.sendAgain": "Send again",
  "people.detail.utang": "Utang",
  "people.detail.iLent": "I lent money",
  "people.detail.iBorrowed": "I borrowed money",
  "people.detail.amount": "Amount",
  "people.detail.amountInvalid": "Enter a valid amount.",
  "people.detail.confirmUtang": "Continue",
  "people.detail.relationship": "Relationship",
  "people.detail.connectedSince": "Connected since {date}",
  "invitations.title": "Invitations",
  "invitations.received": "Received",
  "invitations.sent": "Sent",
  "invitations.emptyTitle": "No pending invitations",
  "invitations.emptyBody": "When someone invites you to a Personal Utang, it appears here.",
  "invitations.sentEmpty": "No outgoing pending invitations.",
  "invitations.personalUtangRequest": "Personal Utang request",
  "invitations.waitingResponse": "Waiting for response",
  "invitations.someone": "Someone",
  "invitations.respondTitle": "Respond with invitation link",
  "invitations.respondHelp":
    "Accept and Decline need the invitation token from the invitation link. Listing alone does not expose the token.",
  "invitations.tokenLabel": "Invitation token",
  "invitations.tokenRequired": "Invitation token is required.",
  "invitations.accept": "Accept",
  "invitations.decline": "Decline",
  "notifications.title": "Notifications",
  "notifications.emptyTitle": "No notifications",
  "notifications.emptyBody": "Alerts about Personal Utang requests appear here.",
  "notifications.unread": "Unread",
  "notifications.markRead": "Mark read",
};

export const filPH: Record<MessageKey, string> = {
  "app.name": "ExItS Mobile",
  "app.skipToContent": "Laktawan papunta sa nilalaman",
  "shell.settings": "Mga setting",
  "shell.back": "Bumalik",
  "nav.personal": "Personal na nabigasyon",
  "nav.home": "Home",
  "nav.people": "Mga tao",
  "nav.invitations": "Mga imbitasyon",
  "nav.notifications": "Mga alerto",
  "home.title": "ExItS Mobile",
  "home.tagline":
    "Ang iyong personal at business na karanasan sa ExItS, para sa telepono, tablet, at desktop.",
  "home.welcome": "Maligayang pagdating,",
  "home.openPersonal": "Buksan ang Personal",
  "empty.title": "Wala pang ipapakita",
  "empty.body": "Walang ipinapakitang listahan sa ngayon.",
  "loading.label": "Naglo-load",
  "error.title": "May nangyaring mali",
  "error.body": "Hindi natapos ang operasyon.",
  "error.reset": "Subukan ulit",
  "diagnostics.copy": "Kopyahin",
  "diagnostics.copied": "Nakopya",
  "theme.label": "Tema",
  "theme.system": "System",
  "theme.light": "Light",
  "theme.dark": "Dark",
  "locale.label": "Wika",
  "locale.en": "English",
  "locale.filPH": "Filipino",
  "appearance.title": "Hitsura",
  "pwa.updateAvailable": "May bagong bersyon",
  "pwa.refresh": "I-refresh",
  "auth.checking": "Tinitingnan ang session",
  "auth.signInTitle": "Mag-sign in",
  "auth.signInSubtitle": "Gamitin ang iyong ExItS account.",
  "auth.username": "Email o username",
  "auth.password": "Password",
  "auth.showPassword": "Ipakita ang password",
  "auth.hidePassword": "Itago ang password",
  "auth.submit": "Mag-sign in",
  "auth.invalidCredentials": "Mali ang email o password.",
  "auth.signInFailed": "Hindi makapag-sign in. Subukan ulit.",
  "auth.rateLimited": "Sobra na ang mga pagtatangka. Subukan ulit mamaya.",
  "auth.signedInAs": "Naka-sign in bilang",
  "auth.signOut": "Mag-sign out",
  "people.title": "Mga tao",
  "people.add": "Magdagdag",
  "people.addPerson": "Magdagdag ng tao",
  "people.search": "Maghanap ng tao",
  "people.searchPlaceholder": "Maghanap ng tao...",
  "people.emptyTitle": "Wala pang tao",
  "people.emptyBody":
    "Magdagdag ng taong pinapahiram, hinuhiram, o sinusubaybayan mo.",
  "people.loadError": "Hindi ma-load ang mga tao.",
  "people.localContact": "Lokal na contact",
  "people.status.notConnected": "Hindi konektado",
  "people.status.requestPending": "Nakabinbing request",
  "people.status.connected": "Konektado",
  "people.add.lede": "Hanapin gamit ang eksaktong ExItS ID o QR payload.",
  "people.add.scanQr": "I-scan ang QR",
  "people.add.scanHint": "I-paste ang QR payload sa ExItS ID, tapos hanapin.",
  "people.add.exitsId": "ExItS ID",
  "people.add.exitsIdPlaceholder": "EX-____-____",
  "people.add.find": "Hanapin ang tao",
  "people.add.requiredId": "Ilagay ang eksaktong ExItS ID.",
  "people.add.notFound": "Walang nahanap na ExItS identity.",
  "people.add.cannotAddSelf": "Hindi mo maaaring idagdag ang sarili mo.",
  "people.add.identityFound": "May nahanap na identity",
  "people.add.cancel": "Kanselahin",
  "people.add.confirm": "Idagdag ang tao",
  "people.detail.notFoundTitle": "Hindi nahanap ang tao",
  "people.detail.notFoundBody": "Wala ang taong ito sa iyong listahan.",
  "people.detail.notConnectedHelp":
    "Pwede mo siyang itago sa Mga tao nang hindi nagbabahagi ng Utang.",
  "people.detail.waitingTitle": "Nakabinbing request",
  "people.detail.waitingBody": "Naghihintay ng sagot mula kay {name}.",
  "people.detail.sentOn": "Ipinadala noong {date}",
  "people.detail.cancelRequest": "Kanselahin ang request",
  "people.detail.sendAgain": "Ipadala ulit",
  "people.detail.utang": "Utang",
  "people.detail.iLent": "Nagpahiram ako",
  "people.detail.iBorrowed": "Humiram ako",
  "people.detail.amount": "Halaga",
  "people.detail.amountInvalid": "Maglagay ng wastong halaga.",
  "people.detail.confirmUtang": "Magpatuloy",
  "people.detail.relationship": "Relasyon",
  "people.detail.connectedSince": "Konektado mula {date}",
  "invitations.title": "Mga imbitasyon",
  "invitations.received": "Natanggap",
  "invitations.sent": "Ipinadala",
  "invitations.emptyTitle": "Walang nakabinbing imbitasyon",
  "invitations.emptyBody": "Kapag may Personal Utang invitation, lalabas ito rito.",
  "invitations.sentEmpty": "Walang papalabas na nakabinbing imbitasyon.",
  "invitations.personalUtangRequest": "Personal Utang request",
  "invitations.waitingResponse": "Naghihintay ng sagot",
  "invitations.someone": "Isang tao",
  "invitations.respondTitle": "Tumugon gamit ang invitation link",
  "invitations.respondHelp":
    "Kailangan ang invitation token mula sa link. Hindi ito ibinibigay ng listahan.",
  "invitations.tokenLabel": "Invitation token",
  "invitations.tokenRequired": "Kailangan ang invitation token.",
  "invitations.accept": "Tanggapin",
  "invitations.decline": "Tanggihan",
  "notifications.title": "Mga notification",
  "notifications.emptyTitle": "Walang notification",
  "notifications.emptyBody": "Lalabas dito ang mga alerto tungkol sa Personal Utang.",
  "notifications.unread": "Hindi pa nabasa",
  "notifications.markRead": "Markahan bilang nabasa",
};

export const catalogs: Record<"en" | "fil-PH", Record<MessageKey, string>> = {
  en,
  "fil-PH": filPH,
};
