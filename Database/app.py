from flask import Flask, request, jsonify
import sqlite3

app = Flask(__name__)
DATABASE = 'leaderboard.db'


def get_db_connection():
    '''
    Helper to get database connection
    '''
    conn = sqlite3.connect(DATABASE)
    conn.row_factory = sqlite3.Row  # Enables dict-like access
    return conn

# POST /submit_score
@app.route('/submit_score', methods=['POST'])
def submit_score():
    data = request.get_json()
    
    if not data or 'player_name' not in data or 'score' not in data:
        return jsonify({'error': 'Missing player_name or score'}), 400
    
    name = data['player_name']
    score = data['score']
    
    conn = get_db_connection()
    conn.execute(
        "INSERT INTO leaderboard (player_name, score) VALUES (?, ?)",
        (name, score)
    )
    conn.commit()
    conn.close
    
    return jsonify({'message': 'Score submitted;'}), 201

# GET /top_scores
@app.route('/top_scores', methods = ['GET'])
def top_scores():
    conn = get_db_connection()
    rows = conn.execute(
        "SELECT player_name, score FROM leaderboard ORDER BY score ASC LIMIT 10"
    ).fetchall()
    conn.close()
    
    # Convert rows to dicts
    scores = [{'player_name': row['player_name'], 'score': row['score']} for row in rows]
    return jsonify(scores)

@app.route("/status", methods=["GET"])
def status():
    return jsonify({"status": "ok"}), 200

if __name__==('__main__'):
    app.run(debug = True)
    