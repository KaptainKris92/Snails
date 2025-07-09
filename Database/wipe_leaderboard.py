import sqlite3

DB_PATH = "leaderboard.db"

def wipe_leaderboard():
    confirm = input("⚠️ This will delete all leaderboard entries. Type 'yes' to continue: ")
    if confirm.lower() != "yes":
        print("Aborted.")
        return

    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()

    cursor.execute("DELETE FROM leaderboard;")
    conn.commit()
    conn.close()

    print("✅ Leaderboard has been wiped.")

if __name__ == "__main__":
    wipe_leaderboard()
