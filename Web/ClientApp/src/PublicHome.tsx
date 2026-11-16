import { useState } from 'react';
import { Brand, Icon } from './components';
import { EmailLogin } from './EmailLogin';
import { readLoginChallenge, useLoginSession } from './loginSession';

export function PublicHome({ onLoggedIn }: { onLoggedIn: () => void }) {
  const [showLogin, setShowLogin] = useState(() => readLoginChallenge() !== null);
  const session = useLoginSession();

  function openLogin() {
    setShowLogin(true);
  }

  return (
    <div className="welcome">
      <header className="welcome-header">
        <Brand />

        <button className="button secondary" onClick={openLogin} aria-expanded={showLogin} aria-controls="public-login">
          Log in <Icon name="arrow" size={17} />
        </button>
      </header>

      <main className="welcome-content">
        <section className="welcome-hero" aria-labelledby="welcome-title">
          <span className="welcome-label"><span /> A place for every team</span>
          <h1 id="welcome-title">Your teams.<br />One <em>workspace.</em></h1>

          <p>Create a home for your business, bring your organizations together, and give people the access they need.</p>

          {!showLogin && (
            <button className="button primary large" onClick={openLogin} aria-controls="public-login">
              Log in to your workspace <Icon name="arrow" />
            </button>
          )}
        </section>

        {showLogin && (
          <div id="public-login">
            <EmailLogin key={session.sessionId ?? 'preparing'} session={session} onLoggedIn={onLoggedIn} />
          </div>
        )}

        <section className="welcome-features" aria-label="Workspace features">
          <article>
            <Icon name="building" />
            <h2>Start with a tenant</h2>
            <p>Keep each business and its organizations in a separate space.</p>
          </article>

          <article>
            <Icon name="teams" />
            <h2>Bring people together</h2>
            <p>Invite members and organize work around your teams.</p>
          </article>

          <article>
            <Icon name="shield" />
            <h2>Make access clear</h2>
            <p>Choose roles and permissions for each workspace.</p>
          </article>
        </section>
      </main>

      <footer className="welcome-footer">VSA Template <span>Built around your organization.</span></footer>
    </div>
  );
}
