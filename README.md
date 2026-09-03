**1. Project summary**



project name: Pong Rivals

Unity multiplayer Pong prototype

two-player client-host architecture

UGS Authentication, Multiplayer Services/Relay, Cloud Save and Cloud Code

persistent account/cosmetic data and pair-specific match records





**2. Unity/version requirements**



Unity 6.3 LTS / 6000.3.7f1



Key packages:

Netcode for GameObjects 2.13.2

Unity Transport 2.7.3

Multiplayer Play Mode 2.0.2

Multiplayer Tools 2.2.11

Multiplayer Services 2.3.1

Authentication 3.7.0

Cloud Save 3.4.1

Cloud Code





**3. Opening the project**



1\. Clone/download the repository.

2\. Open it through Unity Hub using Unity 6.3 LTS.

3\. Allow Package Manager to restore packages.

4\. Open the Bootstrap scene.

5\. Do not begin testing directly from Menus or GameSession.





**4. Unity Gaming Services requirement**





This project relies on its linked Unity Cloud project and uses:



\-Authentication

\-Multiplayer Services / Relay

\-Cloud Save

\-Cloud Code



Cloud Code's PongBackend module must be deployed to the same Unity Services environment used by the Editor project.



UGS-backed functionality requires access to the linked Unity Cloud project/services.





**5. Running one Editor instance**



1\. Open Bootstrap.

2\. Press Play.

3\. Bootstrap automatically loads Menus.

4\. Wait for UGS authentication/player-data initialization.

5\. The Network Test panel should report UGS READY.





**6. Running multiplayer in the Editor**



1\. Stop Play Mode.

2\. Open Bootstrap.

3\. Enable Player 2 in Multiplayer Play Mode BEFORE pressing Play.

4\. Press Play.

5\. Both Editor instances should transition from Bootstrap to Menus.

6\. In the Main Editor, select Create Relay Session.

7\. Read the generated join code.

8\. In Player 2, enter that code and select Join Relay Session.

9\. Confirm one instance reports HOST and the other CLIENT.

10\. On the Host, select Load GameSession.

11\. The Client follows automatically through NGO scene synchronization.





**7. Controls**





Host / Left player:

vertical UI slider controls the left paddle.



Client / Right player:

vertical UI slider controls the right paddle.



On score ball resets → 3-2-1 countdown → next serve.



First player to 3 points wins.





**8. End-of-match behavior**



After a completed match:

\- result is settled through Cloud Code;

\- winner receives 100 currency and one win;

\- loser receives 50 currency and one loss;

\- pair-specific record is updated;

\- both players may request a rematch;

\- rematch only starts when both consent;

\- either player may leave the multiplayer session.





**9. Testing persistent data**



Account data persisted through Cloud Save includes:

\- currency

\- overall wins/losses

\- owned cosmetics

\- equipped cosmetics



Restarting Play Mode with the same anonymous authentication profile should reload the same data.





**10. Scene structure**



Build scene order:



0 Bootstrap

1 Menus

2 GameSession



Bootstrap

→ persistent networking/services



Menus

→ account/store/friends/network test UI



GameSession

→ multiplayer Pong





**11. Known limitations**



\- No final Android APK was produced before submission.

\- Final physical two-device Android validation was not completed.

\- Multiplayer was developed/tested primarily using Unity Multiplayer Play Mode.

\- Friend challenge transmission is represented by prototype/local UI;

&#x20; Internet session establishment currently uses Relay join codes.

\- No host migration.

\- No automatic reconnection after unexpected host/network failure.





**12. Repository notes**



\- Unity-generated files are excluded through .gitignore.

\- Packages are restored through Package Manager.

\- Cloud Code C# module source is included in the repository.





**13. AI-development declaration**



Code structure, refactoring and project strategy

network architecture

NGO/Relay integration

Cloud Save/Cloud Code implementation

debugging runtime/backend errors

