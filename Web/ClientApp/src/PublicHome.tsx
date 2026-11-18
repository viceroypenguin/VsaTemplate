import { useState } from 'react';
import type { ReactNode } from 'react';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Brand, Icon } from './components';
import { EmailLogin } from './EmailLogin';
import { readLoginChallenge, useLoginSession } from './loginSession';

export function PublicHome({ onLoggedIn, themeControl }: { onLoggedIn: () => void; themeControl: ReactNode }) {
  const [showLogin, setShowLogin] = useState(() => readLoginChallenge() !== null);
  const session = useLoginSession();

  function openLogin() {
    setShowLogin(true);
  }

  return (
    <div className="welcome">
      <header className="welcome-header">
        <Brand />

        <div className="header-actions">
          {themeControl}
          <Button variant="outline" onClick={openLogin} aria-expanded={showLogin} aria-controls="public-login">
            Log in <Icon name="arrow" size={17} />
          </Button>
        </div>
      </header>

      <main className="welcome-content">
        <section className="welcome-hero" aria-labelledby="welcome-title">
          <span className="welcome-label"><span /> A place for every team</span>
          <h1 id="welcome-title">Your teams.<br />One <em>workspace.</em></h1>

          <p>Create a home for your business, bring your organizations together, and give people the access they need.</p>

          {!showLogin && (
            <Button size="lg" className="h-12 text-sm" onClick={openLogin} aria-controls="public-login">
              Log in to your workspace <Icon name="arrow" />
            </Button>
          )}
        </section>

        {showLogin && (
          <div id="public-login">
            <EmailLogin key={session.sessionId ?? 'preparing'} session={session} onLoggedIn={onLoggedIn} />
          </div>
        )}

        <section className="welcome-features" aria-label="Workspace features">
          <Card className="welcome-feature" role="article">
            <Icon name="building" />
            <h2>Start with a tenant</h2>
            <p>Keep each business and its organizations in a separate space.</p>
          </Card>

          <Card className="welcome-feature" role="article">
            <Icon name="teams" />
            <h2>Bring people together</h2>
            <p>Invite members and organize work around your teams.</p>
          </Card>

          <Card className="welcome-feature" role="article">
            <Icon name="shield" />
            <h2>Make access clear</h2>
            <p>Choose roles and permissions for each workspace.</p>
          </Card>
        </section>
      </main>

      <footer className="welcome-footer">
        VSA Template
        <span className="welcome-footer-note">Built around your organization.</span>
      </footer>
    </div>
  );
}
