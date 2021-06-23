import { Component } from '@angular/core';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
})
export class HomeComponent {
  constructor() {
    window.location.href = "https://auth.tesla.com/oauth2/v3/authorize?client_id=ownerapi&code_challenge=NjljNWUzMWQ0N2U4NDA3YTRkYzBkZTY5YjE4NWZhMmU2NWViMmU2YTg1NTc2OTA5ODQ5ZTUwM2RlODQ5YWJiNg%3d%3d&code_challenge_method=S256&redirect_uri=https%3a%2f%2fwww.localhost.com:6001%2fvoid%2fcallback&response_type=code&scope=openid+email+offline_access&state=UkXGdE5vHFT8yIn6g9LN&login_hint=hk.turboyang%40gmail.com";
  }
}
