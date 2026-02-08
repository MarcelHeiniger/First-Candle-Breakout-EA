#!/bin/bash

# First Candle Breakout EA - GitHub Upload Script
# This script will help you upload all files to your GitHub repository

echo "========================================="
echo "First Candle Breakout EA"
echo "GitHub Upload Script"
echo "========================================="
echo ""

# Check if git is initialized
if [ ! -d ".git" ]; then
    echo "Initializing git repository..."
    git init
    git branch -M main
    git remote add origin https://github.com/MarcelHeiniger/First-Candle-Breakout-EA.git
    echo "✓ Git initialized"
else
    echo "✓ Git already initialized"
fi

echo ""
echo "Files to be uploaded:"
echo "---------------------"
git status --short
echo ""

# Add all files
echo "Adding files to git..."
git add .

# Show what will be committed
echo ""
echo "Files staged for commit:"
echo "------------------------"
git status --short
echo ""

# Commit
read -p "Enter commit message (default: 'Initial commit - First Candle Breakout EA v1.0.0'): " commit_msg
commit_msg=${commit_msg:-"Initial commit - First Candle Breakout EA v1.0.0"}

git commit -m "$commit_msg"
echo "✓ Files committed"
echo ""

# Push to GitHub
echo "Pushing to GitHub..."
echo "You may be prompted for your GitHub credentials"
echo ""

git push -u origin main

if [ $? -eq 0 ]; then
    echo ""
    echo "========================================="
    echo "✓ SUCCESS!"
    echo "========================================="
    echo ""
    echo "Your EA has been uploaded to:"
    echo "https://github.com/MarcelHeiniger/First-Candle-Breakout-EA"
    echo ""
    echo "Next steps:"
    echo "1. Visit the repository URL above"
    echo "2. Verify all files are there"
    echo "3. Consider creating a release (v1.0.0)"
    echo "4. Share with the trading community!"
    echo ""
else
    echo ""
    echo "========================================="
    echo "⚠ ERROR"
    echo "========================================="
    echo ""
    echo "Failed to push to GitHub."
    echo "Common issues:"
    echo "1. Authentication failed - make sure you have access"
    echo "2. Remote already exists - check your git remote settings"
    echo "3. Network issues - check your connection"
    echo ""
    echo "Try manual push with:"
    echo "git push -u origin main"
    echo ""
fi
